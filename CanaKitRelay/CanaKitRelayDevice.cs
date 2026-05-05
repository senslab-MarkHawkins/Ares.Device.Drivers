using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using Ares.Toolkit.Serial;
using CanaKitRelay.Commands.Requests;
using CanaKitRelay.Commands.Responses;
using CanaKitRelay.Enums;
using CanaKitRelay.Simulation;
using Microsoft.Extensions.Logging;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CanaKitRelay;

public class CanaKitRelayDevice : AresDevice
{
    private readonly IAresSerialConnection _serialConnection;
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private CancellationTokenSource _updateLoopTokenSource = new();
    private Task _updateTask = Task.CompletedTask;
    private readonly ILogger _logger;
    private readonly CompositeDisposable _disposables = new();

    private bool _relay1On;
    private bool _relay2On;

    public CanaKitRelayDevice(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        _logger = logger;
        StateStream = _stateSubject.AsObservable();

        var serialInfo = connectionInfo.SerialConnectionInfo;

        if (connectionInfo.Simulated)
        {
            _serialConnection = new SimCanaKitRelayConnection(serialInfo.PortName);
        }
        else
        {
            _serialConnection = new AresHardwareConnection(
                new SerialPortConnectionInfo(115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One),
                serialInfo.PortName,
                new SerialConnectionOptions
                {
                    SendBuffer = TimeSpan.FromMilliseconds(50),
                    SendTimeout = TimeSpan.FromSeconds(2)
                });
        }

        _disposables.Add(_serialConnection.GetTransactionStream<RelayStateResponse>()
            .Select(t => t.Response)
            .Subscribe(UpdateRelayState));
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override async Task<bool> Activate(CancellationToken ct)
    {
        try
        {
            _serialConnection.AttemptOpen();
            
            // Validate connection
            await _serialConnection.Send(new RelayPingRequest());

            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "CanaKit Relay Active" };
            StartUpdateLoop();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate CanaKit Relay");
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
                    await _serialConnection.Send(new RelayGetRequest(1));
                    await Task.Delay(100);
                    await _serialConnection.Send(new RelayGetRequest(2));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error polling CanaKit Relay state");
                }
                await Task.Delay(TimeSpan.FromSeconds(5), _updateLoopTokenSource.Token);
            }
        }, _updateLoopTokenSource.Token);
    }

    private void UpdateRelayState(RelayStateResponse response)
    {
        if (response.RelayNumber == 1) _relay1On = response.IsOn;
        if (response.RelayNumber == 2) _relay2On = response.IsOn;

        var next = AresStateBuilder.Create()
            .Add("Relay1On", _relay1On)
            .Add("Relay2On", _relay2On)
            .Add("Bypass", !_relay1On && !_relay2On)
            .Build();
        _stateSubject.OnNext(next);
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        if (!Enum.TryParse<CanaKitRelayCommand>(command, out var deviceCommandEnum))
        {
            return new CommandResult { Success = false, Error = $"Invalid or unsupported command: '{command}'" };
        }

        try
        {
            switch (deviceCommandEnum)
            {
                case CanaKitRelayCommand.SetBypass:
                    await _serialConnection.Send(new RelayOffRequest(1));
                    await _serialConnection.Send(new RelayOffRequest(2));
                    break;
                case CanaKitRelayCommand.SetFullVacuum:
                    await _serialConnection.Send(new RelayOnRequest(1));
                    await _serialConnection.Send(new RelayOnRequest(2));
                    break;
                case CanaKitRelayCommand.ToggleRelay1:
                    if (_relay1On) await _serialConnection.Send(new RelayOffRequest(1));
                    else await _serialConnection.Send(new RelayOnRequest(1));
                    break;
                case CanaKitRelayCommand.ToggleRelay2:
                    if (_relay2On) await _serialConnection.Send(new RelayOffRequest(2));
                    else await _serialConnection.Send(new RelayOnRequest(2));
                    break;
                case CanaKitRelayCommand.GetState:
                    await _serialConnection.Send(new RelayGetRequest(1));
                    await _serialConnection.Send(new RelayGetRequest(2));
                    break;
            }
            return new CommandResult { Success = true };
        }
        catch (Exception ex)
        {
            return new CommandResult { Success = false, Error = ex.Message };
        }
    }

    public override Task UpdateSettings(AresStruct settings) => Task.CompletedTask;

    public override Task<AresStruct> GetSettings() => Task.FromResult(new AresStruct());

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        return Task.FromResult(new List<DeviceCommandDescriptor>
        {
            new() { Name = CanaKitRelayCommand.SetBypass.ToString(), Description = "Sets both relays to OFF (Bypass mode)" },
            new() { Name = CanaKitRelayCommand.SetFullVacuum.ToString(), Description = "Sets both relays to ON (Full Vacuum mode)" },
            new() { Name = CanaKitRelayCommand.ToggleRelay1.ToString(), Description = "Toggles Relay 1" },
            new() { Name = CanaKitRelayCommand.ToggleRelay2.ToString(), Description = "Toggles Relay 2" },
            new() { Name = CanaKitRelayCommand.GetState.ToString(), Description = "Forces a relay state update" }
        });
    }

    public override async Task EnterSafeMode(CancellationToken ct)
    {
        await ExecuteCommand(CanaKitRelayCommand.SetBypass.ToString(), [], ct);
    }

    public async ValueTask DisposeAsync()
    {
        _updateLoopTokenSource.Cancel();
        await _updateTask;
        _updateLoopTokenSource.Dispose();
        _disposables.Dispose();
        await _serialConnection.DisposeAsync();
    }
}
