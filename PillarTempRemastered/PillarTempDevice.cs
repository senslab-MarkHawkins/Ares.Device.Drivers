using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Microsoft.Extensions.Logging;
using PillarTempRemastered.PID;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PillarTempRemastered;

public class PillarTempDevice : AresDevice
{
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private readonly SignalMeasurePIDController _pidController;
    private double _targetTemperature;
    private double _calculatedOutput;
    private string _status = "INACTIVE";

    public PillarTempDevice(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        StateStream = _stateSubject.AsObservable();

        var p = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("P")?.NumberValue ?? 0.001346;
        var i = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("I")?.NumberValue ?? 0.00035;
        var d = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("D")?.NumberValue ?? 0.0;
        var minOut = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("MinOutput")?.NumberValue ?? 0.01;
        var maxOut = connectionInfo.DeviceSettings.Fields.GetValueOrDefault("MaxOutput")?.NumberValue ?? 2.0;
        var sigAvg = (int)(connectionInfo.DeviceSettings.Fields.GetValueOrDefault("PastSignalsToAvg")?.NumberValue ?? 5);
        var measAvg = (int)(connectionInfo.DeviceSettings.Fields.GetValueOrDefault("PastMeasurementsToAvg")?.NumberValue ?? 1);

        _pidController = new SignalMeasurePIDController(sigAvg, measAvg)
        {
            Kp = p,
            Ki = i,
            Kd = d,
            Min = minOut,
            Max = maxOut,
            ChangeLimit = 0.05 // Legacy default
        };

        StateSchema = AresSchemaBuilder.Empty()
            .AddEntry("TargetTemperature", AresSchemaBuilder.NumberEntry().Build())
            .AddEntry("CalculatedOutput", AresSchemaBuilder.NumberEntry().Build())
            .AddEntry("Status", AresSchemaBuilder.StringEntry().Build())
            .AddEntry("ProportionalContribution", AresSchemaBuilder.NumberEntry().Build())
            .AddEntry("IntegralContribution", AresSchemaBuilder.NumberEntry().Build())
            .AddEntry("DerivativeContribution", AresSchemaBuilder.NumberEntry().Build())
            .Build();

        UpdateState();
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override Task<bool> Activate(CancellationToken ct)
    {
        _status = "READY";
        UpdateState();
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Pillar Temp PID is active" };
        return Task.FromResult(true);
    }

    public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);

    public override Task<AresStruct> GetSettings()
    {
        return Task.FromResult(AresStateBuilder.Create()
            .Add("P", _pidController.Kp)
            .Add("I", _pidController.Ki)
            .Add("D", _pidController.Kd)
            .Add("MinOutput", _pidController.Min)
            .Add("MaxOutput", _pidController.Max)
            .Build());
    }

    public override Task UpdateSettings(AresStruct settings)
    {
        if (settings.Fields.TryGetValue("P", out var p)) _pidController.Kp = p.NumberValue;
        if (settings.Fields.TryGetValue("I", out var i)) _pidController.Ki = i.NumberValue;
        if (settings.Fields.TryGetValue("D", out var d)) _pidController.Kd = d.NumberValue;
        return Task.CompletedTask;
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        try
        {
            switch (command)
            {
                case "SetTargetTemperature":
                    var target = GetArg(arguments, "Target")?.NumberValue ?? 0;
                    _targetTemperature = target;
                    if (target > 0) _status = "ACTIVE";
                    else _status = "INACTIVE";
                    _pidController.Reset();
                    UpdateState();
                    return new CommandResult { Success = true };

                case "ProvideMeasurement":
                    var measured = GetArg(arguments, "Temperature")?.NumberValue ?? 0;
                    if (_status == "ACTIVE")
                    {
                        _calculatedOutput = _pidController.CalculateCommandValue(_targetTemperature, measured);
                    }
                    UpdateState();
                    return new CommandResult 
                    { 
                        Success = true, 
                        Result = AresValueHelper.CreateNumber(_calculatedOutput) 
                    };

                case "Reset":
                    _pidController.Reset();
                    _status = "INACTIVE";
                    _calculatedOutput = 0;
                    UpdateState();
                    return new CommandResult { Success = true };

                default:
                    return new CommandResult { Success = false, Error = $"Unknown command: {command}" };
            }
        }
        catch (Exception ex)
        {
            return new CommandResult { Success = false, Error = ex.Message };
        }
    }

    private void UpdateState()
    {
        var next = AresStateBuilder.Create()
            .Add("TargetTemperature", _targetTemperature)
            .Add("CalculatedOutput", _calculatedOutput)
            .Add("Status", _status)
            .Add("ProportionalContribution", _pidController.ProportionalContribution)
            .Add("IntegralContribution", _pidController.IntegralContribution)
            .Add("DerivativeContribution", _pidController.DerivativeContribution)
            .Build();
        _stateSubject.OnNext(next);
    }

    private AresValue? GetArg(List<DeviceCommandArgument> args, string name) =>
        args.FirstOrDefault(a => a.ArgName == name)?.ArgValue;

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        return Task.FromResult(new List<DeviceCommandDescriptor>
        {
            new()
            {
                Name = "SetTargetTemperature",
                Description = "Sets the desired temperature and starts PID control.",
                InputSchema = AresSchemaBuilder.Empty().AddEntry("Target", AresSchemaBuilder.NumberEntry().Build()).Build()
            },
            new()
            {
                Name = "ProvideMeasurement",
                Description = "Provides the current temperature measurement and returns the calculated output power.",
                InputSchema = AresSchemaBuilder.Empty().AddEntry("Temperature", AresSchemaBuilder.NumberEntry().Build()).Build(),
                OutputSchema = AresSchemaBuilder.NumberEntry().WithDescription("Calculated Output Power").Build()
            },
            new()
            {
                Name = "Reset",
                Description = "Resets the PID controller and sets status to INACTIVE."
            }
        });
    }

    public override Task EnterSafeMode(CancellationToken ct)
    {
        _status = "INACTIVE";
        _calculatedOutput = 0;
        _pidController.Reset();
        UpdateState();
        return Task.CompletedTask;
    }
}
