using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using Lambda103Shutter.Commands;
using Lambda103Shutter.Commands.Requests;
using Lambda103Shutter.Commands.Responses;
using Lambda103Shutter.Simulation;
using Microsoft.Extensions.Logging;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Lambda103Shutter;

public class Lambda103ShutterDevice : AresDevice
{
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private readonly IAresSerialConnection _serialConnection;
    private readonly ILogger _logger;
    private readonly CompositeDisposable _disposables = new();
    private CancellationTokenSource _pollingCts = new();
    private Task _pollingTask = Task.CompletedTask;

    public Lambda103ShutterDevice(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        _logger = logger;
        StateStream = _stateSubject.AsObservable();

        var serialInfo = connectionInfo.SerialConnectionInfo;

        if (connectionInfo.Simulated)
        {
            _serialConnection = new SimLambda103Connection(serialInfo.PortName);
        }
        else
        {
            _serialConnection = new AresHardwareConnection(
                new SerialPortConnectionInfo(128000, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One),
                serialInfo.PortName,
                new SerialConnectionOptions
                {
                    SendBuffer = TimeSpan.FromMilliseconds(50),
                    SendTimeout = TimeSpan.FromSeconds(2)
                });
        }
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override async Task<bool> Activate(CancellationToken ct)
    {
        try
        {
            _serialConnection.AttemptOpen();
            if (!_serialConnection.IsOpen)
            {
                Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = "Failed to open serial port." };
                return false;
            }

            var validation = await Validate();
            if (!validation.Success)
            {
                Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = validation.Message ?? "Validation failed." };
                return false;
            }

            await StartPolling();

            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Lambda 103 Shutter is active." };
            return true;
        }
        catch (Exception ex)
        {
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Activation error: {ex.Message}" };
            return false;
        }
    }

    public override async Task EnterSafeMode(CancellationToken ct)
    {
        await SetShutter(false);
    }

    public override async Task<AresStruct> GetState()
    {
        return _stateSubject.Value;
    }

    private async Task StartPolling()
    {
        _pollingCts.Cancel();
        await _pollingTask;
        _pollingCts = new CancellationTokenSource();

        _pollingTask = Task.Run(async () =>
        {
            while (!_pollingCts.Token.IsCancellationRequested)
            {
                try
                {
                    await UpdateStatus();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during status update.");
                }
                await Task.Delay(TimeSpan.FromSeconds(5), _pollingCts.Token);
            }
        }, _pollingCts.Token);
    }

    private async Task UpdateStatus()
    {
        var request = new StatusRequest();
        var response = await _serialConnection.Send(request);
        
        var nextState = AresStateBuilder.Create()
            .Add("FilterWheel", response.FilterWheel)
            .Add("ShutterOpen", response.ShutterOpen)
            .Build();

        _stateSubject.OnNext(nextState);
    }

    public async Task SetShutter(bool open)
    {
        var request = new SetShutterRequest(open);
        await _serialConnection.Send(request);
        await UpdateStatus();
    }

    public async Task SetWheel(int position)
    {
        var request = new SetWheelRequest(position);
        await _serialConnection.Send(request);
        await UpdateStatus();
    }

    protected async Task<SerialDeviceValidationResult> Validate()
    {
        var request = new ValidationRequest();
        try
        {
            var response = await _serialConnection.Send(request, TimeSpan.FromSeconds(5));
            return new SerialDeviceValidationResult(response.IsValid);
        }
        catch (TimeoutException)
        {
            return new SerialDeviceValidationResult(false, "Timeout during validation.");
        }
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        if (!Enum.TryParse<Lambda103Command>(command, out var cmd))
        {
            return new CommandResult { Success = false, Error = $"Unsupported command: {command}" };
        }

        try
        {
            switch (cmd)
            {
                case Lambda103Command.SetShutter:
                    var open = arguments.FirstOrDefault(a => a.ArgName == "Open")?.ArgValue.BoolValue ?? false;
                    await SetShutter(open);
                    return new CommandResult { Success = true };

                case Lambda103Command.SetWheel:
                    var pos = (int)(arguments.FirstOrDefault(a => a.ArgName == "Position")?.ArgValue.NumberValue ?? 0);
                    await SetWheel(pos);
                    return new CommandResult { Success = true };

                case Lambda103Command.GetStatus:
                    await UpdateStatus();
                    return new CommandResult { Success = true };

                default:
                    return new CommandResult { Success = false, Error = "Command logic missing." };
            }
        }
        catch (Exception ex)
        {
            return new CommandResult { Success = false, Error = ex.Message };
        }
    }

    public override Task UpdateSettings(AresStruct settings)
    {
        return Task.CompletedTask;
    }

    public override Task<AresStruct> GetSettings()
    {
        return Task.FromResult(new AresStruct());
    }

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        return Task.FromResult(new List<DeviceCommandDescriptor>
        {
            new DeviceCommandDescriptor
            {
                Name = Lambda103Command.SetShutter.ToString(),
                Description = "Opens or closes the shutter.",
                InputSchema = AresSchemaBuilder.Empty()
                    .AddEntry("Open", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
                    .Build()
            },
            new DeviceCommandDescriptor
            {
                Name = Lambda103Command.SetWheel.ToString(),
                Description = "Sets the filter wheel position (0-9).",
                InputSchema = AresSchemaBuilder.Empty()
                    .AddEntry("Position", AresSchemaBuilder.NumberEntry().Build())
                    .Build()
            },
            new DeviceCommandDescriptor
            {
                Name = Lambda103Command.GetStatus.ToString(),
                Description = "Forces a status update."
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        _pollingCts.Cancel();
        await _pollingTask;
        _disposables.Dispose();
        await _serialConnection.DisposeAsync();
        _stateSubject.OnCompleted();
    }
}
