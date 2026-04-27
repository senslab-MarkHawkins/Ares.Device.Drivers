using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using Ares.Toolkit.Serial;
using Microsoft.Extensions.Logging;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ShawHygrometer.Commands.Requests;
using ShawHygrometer.Commands.Responses;
using ShawHygrometer.Enums;
using ShawHygrometer.Simulation;

namespace ShawHygrometer;

public class ShawSuperdewHygrometer : AresDevice
{
    private readonly IAresSerialConnection _serialConnection;
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private CancellationTokenSource _updateLoopTokenSource = new();
    private Task _updateTask = Task.CompletedTask;
    private readonly ILogger _logger;
    private CompositeDisposable _disposables = new();

    public ShawSuperdewHygrometer(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        _logger = logger;
        StateStream = _stateSubject.AsObservable();

        var serialInfo = connectionInfo.SerialConnectionInfo;

        if (connectionInfo.Simulated)
        {
            _serialConnection = new SimWaterConnection(serialInfo.PortName);
        }
        else
        {
            _serialConnection = new AresHardwareConnection(
                new SerialPortConnectionInfo(9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One),
                serialInfo.PortName,
                new SerialConnectionOptions
                {
                    SendBuffer = TimeSpan.FromMilliseconds(50),
                    SendTimeout = TimeSpan.FromSeconds(2)
                });
        }

        _disposables.Add(_serialConnection.GetTransactionStream<WaterPpmResponse>()
            .Select(t => t.Response)
            .Subscribe(UpdateLiveData));
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override async Task<bool> Activate(CancellationToken ct)
    {
        try
        {
            _serialConnection.AttemptOpen();
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Shaw Superdew Water Active" };
            StartUpdateLoop();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate Shaw Superdew Water");
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = ex.Message };
            return false;
        }
    }

    public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);

    private void StartUpdateLoop()
    {
        _updateLoopTokenSource = new CancellationTokenSource();
        _updateTask = Task.Run(async () =>
        {
            while (!_updateLoopTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    await _serialConnection.Send(new GetWaterPpmRequest());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error polling Shaw Superdew Water state");
                }
                await Task.Delay(TimeSpan.FromSeconds(1), _updateLoopTokenSource.Token);
            }
        }, _updateLoopTokenSource.Token);
    }

    private void UpdateLiveData(WaterPpmResponse response)
    {
        var next = AresStateBuilder.Create()
            .Add("WaterPpm", response.WaterPpm)
            .Build();
        _stateSubject.OnNext(next);
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        if (!Enum.TryParse<WaterCommand>(command, out var deviceCommandEnum))
        {
            return new CommandResult { Success = false, Error = $"Invalid or unsupported command: '{command}'" };
        }

        if (deviceCommandEnum == WaterCommand.GetWaterPpm)
        {
            await _serialConnection.Send(new GetWaterPpmRequest());
            return new CommandResult { Success = true };
        }

        return new CommandResult { Success = false, Error = "Command not supported by this device" };
    }

    public override Task UpdateSettings(AresStruct settings) => Task.CompletedTask;

    public override Task<AresStruct> GetSettings() => Task.FromResult(new AresStruct());

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        return Task.FromResult(new List<DeviceCommandDescriptor>
        {
            new()
            {
                Name = WaterCommand.GetWaterPpm.ToString(),
                Description = "Forces a water PPM update"
            }
        });
    }

    public override async Task EnterSafeMode(CancellationToken ct) => await Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        _updateLoopTokenSource.Cancel();
        await _updateTask;
        _updateLoopTokenSource.Dispose();
        _disposables.Dispose();
        await _serialConnection.DisposeAsync();
    }
}
