using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using Microsoft.Extensions.Logging;
using MKS651DRemastered.Commands;
using MKS651DRemastered.Commands.Requests;
using MKS651DRemastered.Commands.Responses;
using MKS651DRemastered.Connection;
using MKS651DRemastered.Simulation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MKS651DRemastered;

public class MKS651D : AresDevice
{
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private CancellationTokenSource _updateLoopTokenSource = new();
    private CompositeDisposable _stateWatchers = new();
    private Task _stateUpdater = Task.CompletedTask;
    private readonly IMKS651DConnection _serialConnection;
    private readonly ILogger _logger;

    private double _pressure;
    private double _valvePosition;
    private int _activeSetpoint;
    private double _minPressureRange;
    private double _maxPressureRange;
    private readonly List<SetpointData> _setpoints = new();

    public MKS651D(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        _logger = logger;
        StateStream = _stateSubject.AsObservable();

        _minPressureRange = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("MinPressureRange")?.NumberValue ?? 0.1;
        _maxPressureRange = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("MaxPressureRange")?.NumberValue ?? 1000;

        for (int i = 1; i <= 5; i++)
        {
            _setpoints.Add(new SetpointData { Index = i });
        }

        if (connectionInfo.Simulated)
        {
            _serialConnection = new SimMKS651DConnection(connectionInfo.SerialConnectionInfo.PortName);
        }
        else
        {
            _serialConnection = new MKS651DConnection(connectionInfo.SerialConnectionInfo.PortName);
        }

        StateSchema = AresSchemaBuilder.Empty()
            .AddEntry("Pressure", AresSchemaBuilder.NumberEntry().WithDescription("Current Pressure in Torr").Build())
            .AddEntry("ValvePosition", AresSchemaBuilder.NumberEntry().WithDescription("Current Valve Position in %").Build())
            .AddEntry("ActiveSetpoint", AresSchemaBuilder.NumberEntry().WithDescription("Currently active setpoint index (1-5)").Build())
            .AddEntry("Setpoints", AresSchemaBuilder.Entry(AresDataType.List)
                .WithListElementSchema(element => {
                    element.WithStructSchema(spStruct => {
                        spStruct.Fields.Add("Index", AresSchemaBuilder.NumberEntry().Build());
                        spStruct.Fields.Add("Pressure", AresSchemaBuilder.NumberEntry().Build());
                        spStruct.Fields.Add("Gain", AresSchemaBuilder.NumberEntry().Build());
                        spStruct.Fields.Add("Soft", AresSchemaBuilder.NumberEntry().Build());
                    });
                })
                .Build())
            .Build();
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override Task<AresStruct> GetState()
        => Task.FromResult(_stateSubject.Value);

    public override async Task<bool> Activate(CancellationToken ct)
    {
        try
        {
            await Initialize();
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "MKS651D is active!" };
            return true;
        }
        catch (Exception e)
        {
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to initialize: {e.Message}" };
            return false;
        }
    }

    private async Task Initialize()
    {
        // Initial state
        UpdateState();

        // Start polling
        await StartUpdateLoop(TimeSpan.FromSeconds(2));
    }

    public async Task StartUpdateLoop(TimeSpan interval)
    {
        _updateLoopTokenSource.Cancel();
        _updateLoopTokenSource = new CancellationTokenSource();
        var token = _updateLoopTokenSource.Token;

        _stateUpdater = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await PollData();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during MKS651D polling loop");
                }
                await Task.Delay(interval, token);
            }
        }, token);
    }

    private async Task PollData()
    {
        var pressureResp = await _serialConnection.Send(new GetPressureCommand());
        _pressure = pressureResp.Value;

        var valvePosResp = await _serialConnection.Send(new GetValvePositionCommand());
        _valvePosition = valvePosResp.Value;

        // We could poll setpoints here too, or just when they change.
        // Legacy code polls them every 2 seconds.
        for (int i = 1; i <= 5; i++)
        {
            var sp = _setpoints[i - 1];
            var pResp = await _serialConnection.Send(new GetSetpointPressureCommand(i));
            sp.Pressure = pResp.Value;
            var gResp = await _serialConnection.Send(new GetSetpointGainCommand(i));
            sp.Gain = gResp.Value;
            var sResp = await _serialConnection.Send(new GetSetpointSoftCommand(i));
            sp.Soft = sResp.Value;
        }

        UpdateState();
    }

    private void UpdateState()
    {
        var builder = AresStateBuilder.Create()
            .Add("Pressure", _pressure)
            .Add("ValvePosition", _valvePosition)
            .Add("ActiveSetpoint", _activeSetpoint)
            .AddList("Setpoints", _setpoints, (sp) => AresValueHelper.CreateStruct(AresStateBuilder.Create()
                .Add("Index", sp.Index)
                .Add("Pressure", sp.Pressure)
                .Add("Gain", sp.Gain)
                .Add("Soft", sp.Soft)
                .Build()));

        _stateSubject.OnNext(builder.Build());
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        if (!Enum.TryParse<MKS651DCommand>(command, out var cmdEnum))
        {
            return new CommandResult { Success = false, Error = $"Unknown command: {command}" };
        }

        try
        {
            switch (cmdEnum)
            {
                case MKS651DCommand.OpenValve:
                    await _serialConnection.Send(new OpenValveCommand());
                    break;
                case MKS651DCommand.CloseValve:
                    await _serialConnection.Send(new CloseValveCommand());
                    break;
                case MKS651DCommand.SetSetpointActive:
                    if (GetArg(arguments, "Index") is { HasNumberValue: true, NumberValue: var idx })
                    {
                        await _serialConnection.Send(new SetSetpointActiveCommand((int)idx));
                        _activeSetpoint = (int)idx;
                    }
                    break;
                case MKS651DCommand.SetSetpointPressure:
                    if (GetArg(arguments, "Index") is { HasNumberValue: true, NumberValue: var spIdx } &&
                        GetArg(arguments, "Value") is { HasNumberValue: true, NumberValue: var spVal })
                    {
                        var percentFS = spVal / (_maxPressureRange / 100.0);
                        await _serialConnection.Send(new SetSetpointPressureCommand((int)spIdx, percentFS));
                    }
                    break;
                case MKS651DCommand.SetSetpointGain:
                    if (GetArg(arguments, "Index") is { HasNumberValue: true, NumberValue: var gIdx } &&
                        GetArg(arguments, "Value") is { HasNumberValue: true, NumberValue: var gVal })
                    {
                        await _serialConnection.Send(new SetSetpointGainCommand((int)gIdx, gVal));
                    }
                    break;
                case MKS651DCommand.SetSetpointSoft:
                    if (GetArg(arguments, "Index") is { HasNumberValue: true, NumberValue: var sIdx } &&
                        GetArg(arguments, "Value") is { HasNumberValue: true, NumberValue: var sVal })
                    {
                        await _serialConnection.Send(new SetSetpointSoftCommand((int)sIdx, sVal));
                    }
                    break;
                case MKS651DCommand.SetMaxSensorRange:
                    if (GetArg(arguments, "Value") is { HasNumberValue: true, NumberValue: var maxV })
                    {
                        await _serialConnection.Send(new SetMaxSensorRangeCommand(maxV));
                        _maxPressureRange = maxV;
                    }
                    break;
                case MKS651DCommand.SetMinSensorRange:
                    if (GetArg(arguments, "Value") is { HasNumberValue: true, NumberValue: var minV })
                    {
                        await _serialConnection.Send(new SetMinSensorRangeCommand(minV));
                        _minPressureRange = minV;
                    }
                    break;
                default:
                    return new CommandResult { Success = false, Error = $"Command {cmdEnum} not implemented yet" };
            }
            return new CommandResult { Success = true };
        }
        catch (Exception ex)
        {
            return new CommandResult { Success = false, Error = ex.Message };
        }
    }

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        var descriptors = new List<DeviceCommandDescriptor>
        {
            new() { Name = MKS651DCommand.OpenValve.ToString(), Description = "Opens the pressure valve fully." },
            new() { Name = MKS651DCommand.CloseValve.ToString(), Description = "Closes the pressure valve fully." },
            new() { 
                Name = MKS651DCommand.SetSetpointActive.ToString(), 
                Description = "Selects which setpoint (1-5) is currently controlling the valve.",
                InputSchema = AresSchemaBuilder.Empty().AddEntry("Index", AresSchemaBuilder.NumberEntry().Build()).Build()
            },
            new() {
                Name = MKS651DCommand.SetSetpointPressure.ToString(),
                Description = "Sets the pressure target for a specific setpoint index.",
                InputSchema = AresSchemaBuilder.Empty()
                    .AddEntry("Index", AresSchemaBuilder.NumberEntry().Build())
                    .AddEntry("Value", AresSchemaBuilder.NumberEntry().Build())
                    .Build()
            }
        };
        return Task.FromResult(descriptors);
    }

    private AresValue? GetArg(List<DeviceCommandArgument> args, string name) =>
        args.FirstOrDefault(a => a.ArgName == name)?.ArgValue;

    public override Task EnterSafeMode(CancellationToken ct)
    {
        return _serialConnection.Send(new CloseValveCommand());
    }

    public override async Task UpdateSettings(AresStruct settings)
    {
        if (settings.Fields.TryGetValue("MinPressureRange", out var minVal) && minVal.HasNumberValue)
            _minPressureRange = minVal.NumberValue;
        if (settings.Fields.TryGetValue("MaxPressureRange", out var maxVal) && maxVal.HasNumberValue)
            _maxPressureRange = maxVal.NumberValue;

        await Task.CompletedTask;
    }

    public override Task<AresStruct> GetSettings()
    {
        return Task.FromResult(AresStateBuilder.Create()
            .Add("MinPressureRange", _minPressureRange)
            .Add("MaxPressureRange", _maxPressureRange)
            .Build());
    }

    private class SetpointData
    {
        public int Index { get; set; }
        public double Pressure { get; set; }
        public double Gain { get; set; }
        public double Soft { get; set; }
    }
}
