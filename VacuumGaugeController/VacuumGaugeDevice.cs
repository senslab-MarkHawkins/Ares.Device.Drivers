using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Device;
using Ares.Toolkit.Serial;
using Microsoft.Extensions.Logging;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VacuumGaugeController.Commands.Requests;
using VacuumGaugeController.Commands.Responses;
using VacuumGaugeController.Commands.Responses.Parsers;
using VacuumGaugeController.Enums;
using VacuumGaugeController.Simulation;

using VacuumGaugeController.Commands;

namespace VacuumGaugeController;

public class VacuumGaugeDevice : AresDevice
{
    private readonly IAresSerialConnection _serialConnection;
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private CancellationTokenSource _updateLoopTokenSource = new();
    private Task _updateTask = Task.CompletedTask;
    private readonly ILogger _logger;

    public VacuumGaugeDevice(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        _logger = logger;
        StateStream = _stateSubject.AsObservable();

        var serialInfo = connectionInfo.SerialConnectionInfo;

        if (connectionInfo.Simulated)
        {
            _serialConnection = new SimVacuumGaugeConnection(serialInfo.PortName);
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
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override async Task<bool> Activate(CancellationToken ct)
    {
        try
        {
            _serialConnection.AttemptOpen();
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Vacuum Gauge Active" };
            StartUpdateLoop();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate Vacuum Gauge");
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
                    await UpdateState();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error updating Vacuum Gauge state");
                }
                await Task.Delay(TimeSpan.FromSeconds(5), _updateLoopTokenSource.Token);
            }
        }, _updateLoopTokenSource.Token);
    }

    private async Task UpdateState()
    {
        // Two-step protocol:
        // 1. Send command (PR1)
        // 2. Wait for ACK (0x06) - handled by sending command and checking if we get data or error
        // 3. Send ENQ (0x05) to get actual data
        
        var pr1 = new GetPressureRequest();
        try 
        {
            // We expect an ACK (0x06) first. AresHardwareConnection.Send usually waits for a parsed response.
            // Our protocol is weird. Let's try sending PR1, then sending ENQ immediately after if we want to follow legacy.
            // Or better, use a transaction.
            
            await _serialConnection.Send(pr1); 
            // In a real device, we might need a small delay or check for ACK.
            // Legacy code did: Write(PR1); response = ReadExisting(); if(response[0] != 0x06) GetErrorStatus();
            
            var enq = new GetEnqRequest<PressureResponse>(new PressureParser());
            var response = await _serialConnection.Send(enq);
            
            UpdateStateFromResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pressure from Vacuum Gauge");
            // If failed, try to get error status
            await CheckErrorStatus();
        }
    }

    private async Task CheckErrorStatus()
    {
        var errReq = new GetErrorStatusRequest();
        try
        {
            await _serialConnection.Send(errReq);
            var enq = new GetEnqRequest<ErrorStatusResponse>(new ErrorStatusParser());
            var response = await _serialConnection.Send(enq);
            
            var next = AresStateBuilder.From(_stateSubject.Value)
                .Add("ErrorStatus", response.Status.ToString())
                .Build();
            _stateSubject.OnNext(next);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error status from Vacuum Gauge");
        }
    }

    private void UpdateStateFromResponse(PressureResponse response)
    {
        var next = AresStateBuilder.Create()
            .Add("Pressure", response.Pressure)
            .Add("Status", response.Status.ToString())
            .Build();
        _stateSubject.OnNext(next);
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        if (!Enum.TryParse<VacuumGaugeControllerCommand>(command, out var deviceCommandEnum))
        {
            return new CommandResult
            {
                Success = false,
                Error = $"Invalid or unsupported command: '{command}'"
            };
        }

        switch (deviceCommandEnum)
        {
            case VacuumGaugeControllerCommand.GetPressure:
                await UpdateState();
                return new CommandResult { Success = true };
            case VacuumGaugeControllerCommand.GetErrorStatus:
                await CheckErrorStatus();
                return new CommandResult { Success = true };
            default:
                return new CommandResult { Success = false, Error = "Command logic missing" };
        }
    }

    public override Task UpdateSettings(AresStruct settings) => Task.CompletedTask;

    public override Task<AresStruct> GetSettings() => Task.FromResult(new AresStruct());

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        return Task.FromResult(new List<DeviceCommandDescriptor>
        {
            new()
            {
                Name = VacuumGaugeControllerCommand.GetPressure.ToString(),
                Description = "Forces a pressure update"
            },
            new()
            {
                Name = VacuumGaugeControllerCommand.GetErrorStatus.ToString(),
                Description = "Queries error status"
            }
        });
    }

    public override async Task EnterSafeMode(CancellationToken ct)
    {
        // Vacuum gauge is passive, nothing to do for safe mode
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _updateLoopTokenSource.Cancel();
        await _updateTask;
        _updateLoopTokenSource.Dispose();
        await _serialConnection.DisposeAsync();
    }
}
