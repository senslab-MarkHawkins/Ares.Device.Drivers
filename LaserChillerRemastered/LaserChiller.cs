using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using LaserChillerRemastered.Commands.Requests;
using LaserChillerRemastered.Commands.Responses;
using LaserChillerRemastered.Connection;
using LaserChillerRemastered.Simulation;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace LaserChillerRemastered;

public sealed class LaserChiller : AresDevice, IAsyncDisposable
{
  private const string TargetTemperatureSettingKey = "Target Temperature";
  private readonly ILaserChillerConnection _serialConnection;
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly CompositeDisposable _stateSubscriptions = [];
  private CancellationTokenSource _pollingTokenSource = new();
  private Task _pollingTask = Task.CompletedTask;
  private bool _connected;
  private bool _disposed;

  public LaserChiller(DeviceConnectionInfo connectionInfo) : base(connectionInfo)
  {
    var serialInfo = connectionInfo.SerialConnectionInfo
      ?? throw new InvalidOperationException("Laser chiller requires serial connection information.");

    _serialConnection = connectionInfo.Simulated
      ? new SimLaserChillerConnection(serialInfo.PortName)
      : new LaserChillerConnection(serialInfo.PortName);

    StateStream = _stateSubject.AsObservable();
    _stateSubscriptions.Add(
      _serialConnection.GetTransactionStream<GetManifoldTemperatureResponse>()
        .Select(transaction => transaction.Response)
        .Subscribe(UpdateTemperatureState));

    Type = "Laser Chiller";
    Description = "Serial laser chiller driver packaged for ARES plugin loading.";
    HardwareIdentity = "LaserChiller";
    Version = "1.0.0";

    StateSchema
      .AddEntry("CurrentTemperature", AresDataType.Number, false, "Current manifold temperature.", "C")
      .AddEntry("TargetTemperature", AresDataType.Number, false, "Current target temperature.", "C")
      .AddEntry("Mode", AresDataType.String, true);

    SettingSchema.AddEntry(TargetTemperatureSettingKey, AresDataType.Number, true, "Persisted target temperature.", "C");

    PublishState();
  }

  public override IObservable<AresStruct> StateStream { get; }
  public double CurrentTemperature { get; private set; }
  public double TargetTemperature { get; private set; }
  public string Mode { get; private set; } = "Unknown";

  public async Task SetStabilizedTemperature(double targetTemperature)
  {
    await EnsureConnected();
    await _serialConnection.Send(new SetStabilizedTemperatureCommand(targetTemperature));
    TargetTemperature = targetTemperature;
    PublishState();
  }

  public async Task SetChillerRunMode()
  {
    await EnsureConnected();
    await _serialConnection.Send(new SetRunModeCommand());
    Mode = "Run";
    PublishState();
  }

  public async Task SetChillerStandbyMode()
  {
    await EnsureConnected();
    await _serialConnection.Send(new SetStandbyModeCommand());
    Mode = "Standby";
    PublishState();
  }

  public async Task<double?> GetManifoldTemperature()
  {
    await EnsureConnected();
    var response = await _serialConnection.Send(new GetManifoldTemperatureCommand(), TimeSpan.FromSeconds(5));
    return response.Temperature;
  }

  public override Task<AresStruct> GetState()
    => Task.FromResult(_stateSubject.Value);

  public override Task<AresStruct> GetSettings()
    => Task.FromResult(AresStructHelper.CreateNumberStruct(TargetTemperatureSettingKey, TargetTemperature));

  public override async Task UpdateSettings(AresStruct settings)
  {
    if(settings.Fields.TryGetValue(TargetTemperatureSettingKey, out var targetValue) &&
       targetValue.HasNumberValue &&
       Math.Abs(targetValue.NumberValue - TargetTemperature) > 0.001d)
    {
      await SetStabilizedTemperature(targetValue.NumberValue);
    }
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if(!Enum.TryParse<LaserChillerCommand>(command, out var chillerCommand))
    {
      return new CommandResult
      {
        Success = false,
        Error = $"Invalid or unsupported command: '{command}'"
      };
    }

    try
    {
      switch(chillerCommand)
      {
        case LaserChillerCommand.SetStabilizedTemperature:
          var targetArg = arguments.FirstOrDefault(a => a.ArgName == LaserChillerCommandParameter.TargetTemperature.ToString());
          if(targetArg?.ArgValue is not { HasNumberValue: true, NumberValue: var targetTemperature })
          {
            return new CommandResult
            {
              Success = false,
              Error = $"Command '{command}' requires numeric argument '{LaserChillerCommandParameter.TargetTemperature}'."
            };
          }

          await SetStabilizedTemperature(targetTemperature);
          return new CommandResult { Success = true };

        case LaserChillerCommand.SetChillerRunMode:
          await SetChillerRunMode();
          return new CommandResult { Success = true };

        case LaserChillerCommand.SetChillerStandbyMode:
          await SetChillerStandbyMode();
          return new CommandResult { Success = true };

        case LaserChillerCommand.UpdateManifoldTemperature:
          var currentTemperature = await GetManifoldTemperature();
          return new CommandResult
          {
            Success = true,
            Result = currentTemperature.HasValue ? AresValueHelper.CreateNumber(currentTemperature.Value) : AresValueHelper.CreateNull()
          };

        default:
          return new CommandResult
          {
            Success = false,
            Error = $"Execution logic is missing for '{command}'."
          };
      }
    }
    catch(Exception ex)
    {
      return new CommandResult
      {
        Success = false,
        Error = ex.Message
      };
    }
  }

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      await EnsureConnected();
      var validation = await Validate();
      if(!validation.Success)
      {
        Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = validation.Message };
        return false;
      }

      await PollTemperature();
      await StartPolling(TimeSpan.FromMilliseconds(250));
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = $"Laser chiller {Name} is active." };
      return true;
    }
    catch(Exception ex)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = ex.Message };
      return false;
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
    => SetChillerStandbyMode();

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    var descriptors = new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = LaserChillerCommand.SetStabilizedTemperature.ToString(),
        Description = "Sets the stabilized target temperature for the chiller.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(
            LaserChillerCommandParameter.TargetTemperature.ToString(),
            AresSchemaBuilder.NumberEntry()
              .WithDescription("Desired temperature in degrees Celsius.")
              .Build())
          .Build()
      },
      new()
      {
        Name = LaserChillerCommand.SetChillerRunMode.ToString(),
        Description = "Puts the chiller into run mode."
      },
      new()
      {
        Name = LaserChillerCommand.SetChillerStandbyMode.ToString(),
        Description = "Puts the chiller into standby mode."
      },
      new()
      {
        Name = LaserChillerCommand.UpdateManifoldTemperature.ToString(),
        Description = "Queries the device for the latest manifold temperature.",
        OutputSchema = AresSchemaBuilder.NumberEntry()
          .WithDescription("Current manifold temperature.")
          .Build()
      }
    };

    return Task.FromResult(descriptors);
  }

  public async ValueTask DisposeAsync()
  {
    if(_disposed)
      return;

    _disposed = true;
    _stateSubscriptions.Dispose();
    await _pollingTokenSource.CancelAsync();
    await _pollingTask;
    _pollingTokenSource.Dispose();
    await _serialConnection.DisposeAsync();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
  }

  protected override void Dispose(bool disposing)
  {
    if(disposing && !_disposed)
    {
      DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    base.Dispose(disposing);
  }

  private Task EnsureConnected()
  {
    if(_connected)
      return Task.CompletedTask;

    _serialConnection.AttemptOpen();
    _connected = true;
    return Task.CompletedTask;
  }

  private Task PollTemperature()
    => GetManifoldTemperature();

  private async Task StartPolling(TimeSpan interval)
  {
    await StopPolling();
    _pollingTokenSource.Dispose();
    _pollingTokenSource = new CancellationTokenSource();
    _pollingTask = Task.Run(async () =>
    {
      try
      {
        while(!_pollingTokenSource.IsCancellationRequested)
        {
          try
          {
            await PollTemperature();
          }
          catch(TimeoutException)
          {
            Status = new DeviceOperationalStatus
            {
              OperationalState = OperationalState.Active,
              Message = $"Timed out polling manifold temperature at {DateTime.Now:O}"
            };
          }

          await Task.Delay(interval, _pollingTokenSource.Token);
        }
      }
      catch(OperationCanceledException)
      {
      }
    }, _pollingTokenSource.Token);
  }

  private async Task StopPolling()
  {
    _pollingTokenSource.Cancel();
    try
    {
      await _pollingTask;
    }
    catch(OperationCanceledException)
    {
    }
  }

  private async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      await GetManifoldTemperature();
      return new SerialDeviceValidationResult(true);
    }
    catch(Exception ex)
    {
      return new SerialDeviceValidationResult(false, ex.Message);
    }
  }

  private void UpdateTemperatureState(GetManifoldTemperatureResponse response)
  {
    CurrentTemperature = response.Temperature;
    PublishState();
  }

  private void PublishState()
  {
    _stateSubject.OnNext(
      AresStateBuilder.Create()
        .Add("CurrentTemperature", CurrentTemperature)
        .Add("TargetTemperature", TargetTemperature)
        .Add("Mode", Mode)
        .Build());
  }
}
