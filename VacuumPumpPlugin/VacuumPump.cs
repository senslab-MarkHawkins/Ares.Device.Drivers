using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using Microsoft.Extensions.Logging;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using VacuumPumpPlugin.Commands;
using VacuumPumpPlugin.Commands.Requests;
using VacuumPumpPlugin.Commands.Responses;
using VacuumPumpPlugin.Connection;
using VacuumPumpPlugin.Enums;
using VacuumPumpPlugin.Simulation;

namespace VacuumPumpPlugin;

public class VacuumPump : AresDevice
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private CancellationTokenSource _stateGetterLoopTokenSource = new();
  private CompositeDisposable _stateWatchers = new();
  private Task _stateUpdater = Task.CompletedTask;
  private readonly IVacuumConnection _serialConnection;
  private readonly ILogger _logger;

  private VacuumPumpStatus _pumpStatus;
  private int _rotationSpeed;

  public VacuumPump(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
  {
    StateStream = _stateSubject.AsObservable();
    _logger = logger;
    var serialInfo = connectionInfo.SerialConnectionInfo;

    if (connectionInfo.Simulated)
    {
      _serialConnection = new SimVacuumConnection(serialInfo.PortName);
    }
    else
    {
      _serialConnection = new VacuumConnection(serialInfo.PortName);
    }

    _stateWatchers = new CompositeDisposable
    {
      _serialConnection.GetTransactionStream<PumpStatusResponse>().Select(t => t.Response).Subscribe(UpdatePumpStatus),
      _serialConnection.GetTransactionStream<RotationSpeedResponse>().Select(t => t.Response).Subscribe(UpdateRotationSpeed)
    };
  }

  public override IObservable<AresStruct> StateStream { get; }

  public override Task<AresStruct> GetState() => Task.FromResult(_stateSubject.Value);

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      await StartUpdateLoop(TimeSpan.FromSeconds(3));
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "Vacuum Pump is active" };
      return true;
    }
    catch (Exception e)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = $"Failed to activate: {e.Message}" };
      return false;
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
  {
    return Task.CompletedTask;
  }

  private async Task StartUpdateLoop(TimeSpan interval)
  {
    await StopUpdateLoop();
    _stateGetterLoopTokenSource = new CancellationTokenSource();
    _stateUpdater = Task.Run(async () =>
    {
      while (!_stateGetterLoopTokenSource.IsCancellationRequested)
      {
        try
        {
          await _serialConnection.Send(new GetPumpStatusRequest());
          await Task.Delay(100);
          await _serialConnection.Send(new GetRotationSpeedRequest());
        }
        catch (Exception)
        {
          // Log or handle error
        }
        await Task.Delay(interval, _stateGetterLoopTokenSource.Token);
      }
    }, _stateGetterLoopTokenSource.Token);
  }

  private async Task StopUpdateLoop()
  {
    _stateGetterLoopTokenSource.Cancel();
    await _stateUpdater;
  }

  private void UpdatePumpStatus(PumpStatusResponse response)
  {
    _pumpStatus = response.Status;
    PushState();
  }

  private void UpdateRotationSpeed(RotationSpeedResponse response)
  {
    _rotationSpeed = response.Speed;
    PushState();
  }

  private void PushState()
  {
    var state = AresStateBuilder.Create()
      .Add("PumpStatus", _pumpStatus.ToString())
      .Add("RotationSpeed", _rotationSpeed)
      .Build();
    _stateSubject.OnNext(state);
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if (!Enum.TryParse<VacuumCommand>(command, out var deviceCommandEnum))
    {
      return new CommandResult { Success = false, Error = $"Invalid command: {command}" };
    }

    try
    {
      switch (deviceCommandEnum)
      {
        case VacuumCommand.GetPumpStatus:
          var statusResponse = await _serialConnection.Send(new GetPumpStatusRequest());
          return new CommandResult { Success = true, Result = AresValueHelper.CreateString(statusResponse.Status.ToString()) };
        case VacuumCommand.GetRotationSpeed:
          var speedResponse = await _serialConnection.Send(new GetRotationSpeedRequest());
          return new CommandResult { Success = true, Result = AresValueHelper.CreateNumber(speedResponse.Speed) };
        case VacuumCommand.GetTestResponse:
          var testResponse = await _serialConnection.Send(new GetTestResponseRequest());
          return new CommandResult { Success = true, Result = AresValueHelper.CreateString(testResponse.Response) };
        default:
          return new CommandResult { Success = false, Error = "Command not implemented" };
      }
    }
    catch (Exception ex)
    {
      return new CommandResult { Success = false, Error = ex.Message };
    }
  }

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new() { Name = VacuumCommand.GetPumpStatus.ToString(), Description = "Gets the current pump status" },
      new() { Name = VacuumCommand.GetRotationSpeed.ToString(), Description = "Gets the current rotation speed" },
      new() { Name = VacuumCommand.GetTestResponse.ToString(), Description = "Runs a test command" }
    });
  }

  public override Task<AresStruct> GetSettings()
  {
    throw new NotImplementedException();
  }

  public override Task UpdateSettings(AresStruct settings)
  {
    throw new NotImplementedException();
  }
}
