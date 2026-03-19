using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using SyringePumpNE1000Remastered.Commands.Requests;
using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Connection;
using SyringePumpNE1000Remastered.Simulation;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SyringePumpNE1000Remastered;

public sealed class SyringePumpNE1000Device : AresDevice, IAsyncDisposable
{
  private const string AddressSettingKey = "Address";

  private readonly ISyringePumpConnection _connection;
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly SyringePumpState _currentState;
  private CancellationTokenSource _pollingTokenSource = new();
  private Task _pollingTask = Task.CompletedTask;
  private bool _connected;
  private bool _disposed;

  public SyringePumpNE1000Device(DeviceConnectionInfo info) : base(info)
  {
    AssumedAddress = GetConfiguredAddress(info.DeviceSettings);
    _currentState = new SyringePumpState
    {
      Address = AssumedAddress,
      Status = StatusPrompt.PromptS
    };

    var serialInfo = info.SerialConnectionInfo ?? throw new InvalidOperationException("Syringe pump requires serial connection information.");
    _connection = info.Simulated
      ? new SimSyringePumpConnection(serialInfo.PortName, AssumedAddress)
      : new SyringePumpConnection(serialInfo.PortName);

    StateStream = _stateSubject.AsObservable();

    Type = "Syringe Pump NE1000";
    Description = "New Era NE1000 syringe pump driver packaged for ARES plugin loading.";
    HardwareIdentity = "SyringePumpNE1000";
    Version = "1.0.0";

    StateSchema
      .AddEntry("FirmwareVersion", AresDataType.String, true)
      .AddEntry("Address", AresDataType.Number, false)
      .AddEntry("DiameterMm", AresDataType.Quantity, true, quantitySchema: new QuantitySchema() { QuantityType = QuantityType.Length, BoundsUnit = "Mm" })
      .AddEntry("Status", AresDataType.String, false)
      .AddEntry("DispensedVolume", AresDataType.Quantity, true, quantitySchema: new QuantitySchema() { QuantityType = QuantityType.Volume, BoundsUnit = "mL"})
      .AddEntry("WithdrawnVolume", AresDataType.Quantity, true, quantitySchema: new QuantitySchema() { QuantityType = QuantityType.Volume, BoundsUnit = "mL" })
      .AddEntry("VolumeUnit", AresDataType.String, true)
      .AddEntry("PhaseNumber", AresDataType.Number, true)
      .AddEntry("PhaseFunction", AresDataType.String, true)
      .AddEntry("PhaseRate", AresDataType.Quantity, true, quantitySchema: new QuantitySchema() { QuantityType = QuantityType.VolumeFlow, BoundsUnit = "mL/min" })
      .AddEntry("PhaseRateUnit", AresDataType.String, true)
      .AddEntry("PhaseVolume", AresDataType.Quantity, true, quantitySchema: new QuantitySchema() { QuantityType = QuantityType.Volume, BoundsUnit = "mL" })
      .AddEntry("PhaseVolumeUnit", AresDataType.String, true)
      .AddEntry("PhaseDirection", AresDataType.String, true);

    SettingSchema.AddEntry(AddressSettingKey, AresDataType.Number, false, "Configured pump network address.");

    PublishState();
  }

  public override IObservable<AresStruct> StateStream { get; }
  public int AssumedAddress { get; private set; }

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

      await RefreshState();
      await StartPolling(TimeSpan.FromSeconds(4));
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = $"Syringe pump {Name} is active." };
      return true;
    }
    catch(Exception ex)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = ex.Message };
      return false;
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
    => StopPumpingProgram();

  public override Task<AresStruct> GetState()
    => Task.FromResult(_stateSubject.Value);

  public override Task<AresStruct> GetSettings()
    => Task.FromResult(AresStructHelper.CreateNumberStruct(AddressSettingKey, AssumedAddress));

  public override async Task UpdateSettings(AresStruct settings)
  {
    if(settings.Fields.TryGetValue(AddressSettingKey, out var configuredAddress) &&
       configuredAddress.HasNumberValue)
    {
      var newAddress = (int)Math.Round(configuredAddress.NumberValue);
      if(newAddress != AssumedAddress)
        await SetAddress(newAddress);
    }
  }

  public async Task SetPhase(int phase)
  {
    await EnsureConnected();
    await _connection.Send(new SetPhaseNumberRequest(AssumedAddress, phase), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task SetPhaseFunction(SyringePumpFunction function)
  {
    await EnsureConnected();
    await _connection.Send(new SetPhaseFunctionRequest(AssumedAddress, function), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task SetDiameter(double diameterMm)
  {
    await EnsureConnected();
    await _connection.Send(new SetDiameterRequest(AssumedAddress, diameterMm), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task<double> GetDiameter()
  {
    await EnsureConnected();
    var response = await _connection.Send(new GetDiameterRequest(AssumedAddress), TimeSpan.FromSeconds(6));
    EnsureNoError(response, "GetDiameter");
    _currentState.DiameterMm = response.DiameterMm;
    _currentState.Status = response.Status;
    PublishState();
    return response.DiameterMm;
  }

  public async Task SetProgramFunctionRate(double rateMlPerMinute)
  {
    await EnsureConnected();
    await _connection.Send(new SetPhaseFunctionRateRequest(AssumedAddress, rateMlPerMinute, RateUnit.Mm), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task<PhaseFunctionRateResponse> GetProgramFunctionRate()
  {
    await EnsureConnected();
    var response = await _connection.Send(new GetPhaseFunctionRateRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "GetProgramFunctionRate");
    _currentState.Status = response.Status;
    _currentState.Phase.Rate = response.Rate;
    _currentState.Phase.RateUnit = response.RateUnit;
    PublishState();
    return response;
  }

  public async Task<PhaseFunctionResponse> QueryPhaseFunction()
  {
    await EnsureConnected();
    var response = await _connection.Send(new QueryPhaseFunctionRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "QueryPhaseFunction");
    _currentState.Status = response.Status;
    _currentState.Phase.Function = response.Function;
    PublishState();
    return response;
  }

  public async Task SetProgramFunctionVolumeToBeDispensed(double volumeMl)
  {
    await EnsureConnected();
    await _connection.Send(new SetPhaseFunctionVolumeRequest(AssumedAddress, volumeMl), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task<PhaseFunctionVolumeResponse> GetProgramFunctionVolumeToBeDispensed()
  {
    await EnsureConnected();
    var response = await _connection.Send(new GetPhaseFunctionVolumeRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "GetProgramFunctionVolumeToBeDispensed");
    _currentState.Status = response.Status;
    _currentState.Phase.Volume = response.Volume;
    _currentState.Phase.VolumeUnit = response.VolumeUnit;
    PublishState();
    return response;
  }

  public async Task SetProgramFunctionPumpingDirection(Direction direction)
  {
    await EnsureConnected();
    await _connection.Send(new SetPhaseFunctionDirectionRequest(AssumedAddress, direction), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task<Direction> GetProgramFunctionPumpingDirection()
  {
    await EnsureConnected();
    var response = await _connection.Send(new GetPhaseFunctionDirectionRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "GetProgramFunctionPumpingDirection");
    _currentState.Status = response.Status;
    _currentState.Phase.Direction = response.Direction;
    PublishState();
    return response.Direction;
  }

  public async Task<int> QueryPhase()
  {
    await EnsureConnected();
    var response = await _connection.Send(new PhaseQueryRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "QueryPhase");
    _currentState.Status = response.Status;
    _currentState.Phase.Number = response.Phase;
    PublishState();
    return response.Phase;
  }

  public async Task PurgePump()
  {
    await EnsureConnected();
    await _connection.Send(new PurgeRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task StartPumpingProgram()
  {
    await EnsureConnected();
    if(_currentState.Status is StatusPrompt.PromptI or StatusPrompt.PromptW)
      return;

    await _connection.Send(new StartRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task StopPumpingProgram()
  {
    await EnsureConnected();
    if(_currentState.Status is not (StatusPrompt.PromptI or StatusPrompt.PromptW or StatusPrompt.PromptX))
      return;

    await _connection.Send(new StopRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task<VolumeDispensedResponse> GetVolumeDispensed()
  {
    await EnsureConnected();
    var response = await _connection.Send(new GetVolumeDispensedRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "GetVolumeDispensed");
    _currentState.Status = response.Status;
    _currentState.DispensedVolume = response.Infused;
    _currentState.WithdrawnVolume = response.Withdrawn;
    _currentState.VolumeUnits = response.SystemVolumeUnit;
    PublishState();
    return response;
  }

  public async Task ClearVolumeDispensed(Direction direction)
  {
    if(direction == Direction.UndefinedDirection)
      throw new InvalidOperationException("Cannot clear dispensed volume with an undefined direction.");

    await EnsureConnected();
    await _connection.Send(new ClearVolumeRequest(AssumedAddress, direction), TimeSpan.FromSeconds(3));
    await RefreshState();
  }

  public async Task SetAddress(int address)
  {
    await EnsureConnected();
    await _connection.Send(new SetAddressRequest(address), TimeSpan.FromSeconds(3));
    AssumedAddress = address;
    _currentState.Address = address;
    await RefreshState();
  }

  public async Task<int> GetAddress()
  {
    await EnsureConnected();
    var response = await _connection.Send(new GetAddressRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "GetAddress");
    _currentState.Status = response.Status;
    _currentState.Address = response.RespondingAddress;
    PublishState();
    return response.RespondingAddress;
  }

  public async Task<string> GetFirmwareVersion()
  {
    await EnsureConnected();
    var response = await _connection.Send(new GetFirmwareVersionRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    EnsureNoError(response, "GetFirmwareVersion");
    _currentState.Status = response.Status;
    _currentState.FirmwareVersion = response.FirmwareVersion;
    PublishState();
    return response.FirmwareVersion;
  }

  public async Task RefreshState()
  {
    await EnsureConnected();
    if(string.IsNullOrWhiteSpace(_currentState.FirmwareVersion))
      _currentState.FirmwareVersion = await GetFirmwareVersion();

    var diameter = await _connection.Send(new GetDiameterRequest(AssumedAddress), TimeSpan.FromSeconds(6));
    var dispensedVolume = await _connection.Send(new GetVolumeDispensedRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    var rate = await _connection.Send(new GetPhaseFunctionRateRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    var function = await _connection.Send(new QueryPhaseFunctionRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    var phaseNumber = await _connection.Send(new PhaseQueryRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    var direction = await _connection.Send(new GetPhaseFunctionDirectionRequest(AssumedAddress), TimeSpan.FromSeconds(3));
    var phaseVolume = await _connection.Send(new GetPhaseFunctionVolumeRequest(AssumedAddress), TimeSpan.FromSeconds(3));

    EnsureNoError(diameter, "GetDiameter");
    EnsureNoError(dispensedVolume, "GetVolumeDispensed");
    EnsureNoError(rate, "GetProgramFunctionRate");
    EnsureNoError(function, "QueryPhaseFunction");
    EnsureNoError(phaseNumber, "QueryPhase");
    EnsureNoError(direction, "GetProgramFunctionPumpingDirection");
    EnsureNoError(phaseVolume, "GetProgramFunctionVolumeToBeDispensed");

    _currentState.Address = AssumedAddress;
    _currentState.DiameterMm = diameter.DiameterMm;
    _currentState.DispensedVolume = dispensedVolume.Infused;
    _currentState.WithdrawnVolume = dispensedVolume.Withdrawn;
    _currentState.VolumeUnits = dispensedVolume.SystemVolumeUnit;
    _currentState.Status = rate.Status;
    _currentState.Phase.Number = phaseNumber.Phase;
    _currentState.Phase.Function = function.Function;
    _currentState.Phase.Rate = rate.Rate;
    _currentState.Phase.RateUnit = rate.RateUnit;
    _currentState.Phase.Volume = phaseVolume.Volume;
    _currentState.Phase.VolumeUnit = phaseVolume.VolumeUnit;
    _currentState.Phase.Direction = direction.Direction;
    PublishState();
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if(!Enum.TryParse<SyringePumpNE1000Command>(command, out var parsedCommand))
    {
      return new CommandResult
      {
        Success = false,
        Error = $"Invalid or unsupported command: '{command}'."
      };
    }

    AresValue? GetArg(SyringePumpNE1000CommandParameter param)
      => arguments.FirstOrDefault(a => a.ArgName == param.ToString())?.ArgValue;

    try
    {
      return parsedCommand switch
      {
        SyringePumpNE1000Command.QueryPhaseFunction => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateString((await QueryPhaseFunction()).Function.ToString())
        },
        SyringePumpNE1000Command.SetPhase => await ExecuteNumeric(GetArg(SyringePumpNE1000CommandParameter.Phase), value => SetPhase((int)Math.Round(value)), command, SyringePumpNE1000CommandParameter.Phase),
        SyringePumpNE1000Command.SetPhaseFunction => await ExecuteFunctionCommand(GetArg(SyringePumpNE1000CommandParameter.Function), command),
        SyringePumpNE1000Command.QueryPhase => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateNumber(await QueryPhase())
        },
        SyringePumpNE1000Command.SetDiameter => await ExecuteNumeric(GetArg(SyringePumpNE1000CommandParameter.DiameterMm), SetDiameter, command, SyringePumpNE1000CommandParameter.DiameterMm),
        SyringePumpNE1000Command.GetDiameter => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateNumber(await GetDiameter())
        },
        SyringePumpNE1000Command.SetProgramFunctionRate => await ExecuteNumeric(GetArg(SyringePumpNE1000CommandParameter.RateMlPerMin), SetProgramFunctionRate, command, SyringePumpNE1000CommandParameter.RateMlPerMin),
        SyringePumpNE1000Command.GetProgramFunctionRate => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateStruct(BuildRateStruct(await GetProgramFunctionRate()))
        },
        SyringePumpNE1000Command.SetProgramFunctionVolumeToBeDispensed => await ExecuteNumeric(GetArg(SyringePumpNE1000CommandParameter.VolumeMl), SetProgramFunctionVolumeToBeDispensed, command, SyringePumpNE1000CommandParameter.VolumeMl),
        SyringePumpNE1000Command.GetProgramFunctionVolumeToBeDispensed => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateStruct(BuildVolumeStruct(await GetProgramFunctionVolumeToBeDispensed()))
        },
        SyringePumpNE1000Command.SetProgramFunctionPumpingDirection => await ExecuteDirectionCommand(GetArg(SyringePumpNE1000CommandParameter.Direction), command),
        SyringePumpNE1000Command.GetProgramFunctionPumpingDirection => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateString((await GetProgramFunctionPumpingDirection()).ToString())
        },
        SyringePumpNE1000Command.StartPumpingProgram => await ExecuteNoResult(StartPumpingProgram),
        SyringePumpNE1000Command.PurgePump => await ExecuteNoResult(PurgePump),
        SyringePumpNE1000Command.StopPumpingProgram => await ExecuteNoResult(StopPumpingProgram),
        SyringePumpNE1000Command.GetVolumeDispensed => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateStruct(BuildDispensedVolumeStruct(await GetVolumeDispensed()))
        },
        SyringePumpNE1000Command.ClearVolumeDispensed => await ExecuteNoResult(() => ClearVolumeDispensed(_currentState.Phase.Direction)),
        _ => new CommandResult { Success = false, Error = $"Execution logic is missing for '{command}'." }
      };
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

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    var rateSchema = AresSchemaBuilder.Empty()
      .AddEntry("Rate", AresSchemaBuilder.NumberEntry().Build())
      .AddEntry("RateUnit", AresSchemaBuilder.StringEntry().Build())
      .AddEntry("Status", AresSchemaBuilder.StringEntry().Build())
      .Build();

    var volumeSchema = AresSchemaBuilder.Empty()
      .AddEntry("Volume", AresSchemaBuilder.NumberEntry().Build())
      .AddEntry("VolumeUnit", AresSchemaBuilder.StringEntry().Build())
      .AddEntry("Status", AresSchemaBuilder.StringEntry().Build())
      .Build();

    var dispensedSchema = AresSchemaBuilder.Empty()
      .AddEntry("Infused", AresSchemaBuilder.NumberEntry().Build())
      .AddEntry("Withdrawn", AresSchemaBuilder.NumberEntry().Build())
      .AddEntry("VolumeUnit", AresSchemaBuilder.StringEntry().Build())
      .AddEntry("Status", AresSchemaBuilder.StringEntry().Build())
      .Build();

    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = SyringePumpNE1000Command.QueryPhaseFunction.ToString(),
        Description = "Queries the current program phase function.",
        OutputSchema = AresSchemaBuilder.StringEntry().Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.SetPhase.ToString(),
        Description = "Sets the active program phase number.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(SyringePumpNE1000CommandParameter.Phase.ToString(), AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.SetPhaseFunction.ToString(),
        Description = "Sets the current phase function using the legacy NE1000 function token.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(SyringePumpNE1000CommandParameter.Function.ToString(), AresSchemaBuilder.StringEntry().Build())
          .Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.QueryPhase.ToString(),
        Description = "Queries the current program phase number.",
        OutputSchema = AresSchemaBuilder.NumberEntry().Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.SetDiameter.ToString(),
        Description = "Sets the configured syringe diameter in millimeters.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(SyringePumpNE1000CommandParameter.DiameterMm.ToString(), AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.GetDiameter.ToString(),
        Description = "Gets the configured syringe diameter in millimeters.",
        OutputSchema = AresSchemaBuilder.NumberEntry().Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.SetProgramFunctionRate.ToString(),
        Description = "Sets the current phase rate in the device's MM wire units.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(SyringePumpNE1000CommandParameter.RateMlPerMin.ToString(), AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.GetProgramFunctionRate.ToString(),
        Description = "Gets the current phase rate and units.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct).WithStructSchema(rateSchema).Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.SetProgramFunctionVolumeToBeDispensed.ToString(),
        Description = "Sets the phase dispense volume in milliliters.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(SyringePumpNE1000CommandParameter.VolumeMl.ToString(), AresSchemaBuilder.NumberEntry().Build())
          .Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.GetProgramFunctionVolumeToBeDispensed.ToString(),
        Description = "Gets the programmed dispense volume and unit.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct).WithStructSchema(volumeSchema).Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.SetProgramFunctionPumpingDirection.ToString(),
        Description = "Sets the programmed pumping direction.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(SyringePumpNE1000CommandParameter.Direction.ToString(), AresSchemaBuilder.StringEntry().Build())
          .Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.GetProgramFunctionPumpingDirection.ToString(),
        Description = "Gets the programmed pumping direction.",
        OutputSchema = AresSchemaBuilder.StringEntry().Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.StartPumpingProgram.ToString(),
        Description = "Starts or resumes the pump program."
      },
      new()
      {
        Name = SyringePumpNE1000Command.PurgePump.ToString(),
        Description = "Starts purge mode."
      },
      new()
      {
        Name = SyringePumpNE1000Command.StopPumpingProgram.ToString(),
        Description = "Stops or pauses the pumping program."
      },
      new()
      {
        Name = SyringePumpNE1000Command.GetVolumeDispensed.ToString(),
        Description = "Gets infused and withdrawn dispensed volume totals.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct).WithStructSchema(dispensedSchema).Build()
      },
      new()
      {
        Name = SyringePumpNE1000Command.ClearVolumeDispensed.ToString(),
        Description = "Clears the dispensed volume for the current pumping direction."
      }
    });
  }

  public async ValueTask DisposeAsync()
  {
    if(_disposed)
      return;

    _disposed = true;
    await _pollingTokenSource.CancelAsync();
    try
    {
      await _pollingTask;
    }
    catch(OperationCanceledException)
    {
    }

    _pollingTokenSource.Dispose();
    await _connection.DisposeAsync();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
  }

  protected override void Dispose(bool disposing)
  {
    if(disposing && !_disposed)
      DisposeAsync().AsTask().GetAwaiter().GetResult();

    base.Dispose(disposing);
  }

  private Task EnsureConnected()
  {
    if(_connected)
      return Task.CompletedTask;

    _connection.AttemptOpen();
    _connected = true;
    return Task.CompletedTask;
  }

  private async Task StartPolling(TimeSpan interval)
  {
    await StopPolling();
    _pollingTokenSource.Dispose();
    _pollingTokenSource = new CancellationTokenSource();
    _pollingTask = Task.Run(async () =>
    {
      while(!_pollingTokenSource.IsCancellationRequested)
      {
        try
        {
          await RefreshState();
        }
        catch(TimeoutException)
        {
        }

        await Task.Delay(interval, _pollingTokenSource.Token);
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
    if(_connection is SimSyringePumpConnection)
      return new SerialDeviceValidationResult(true, "Simulated syringe pump detected.");

    try
    {
      await Awaken();
      _currentState.FirmwareVersion = await GetFirmwareVersion();
      await RefreshState();
      return new SerialDeviceValidationResult(true);
    }
    catch(Exception ex)
    {
      return new SerialDeviceValidationResult(false, ex.Message);
    }
  }

  private async Task Awaken()
  {
    try
    {
      await _connection.Send(new GetDiameterRequest(AssumedAddress), TimeSpan.FromSeconds(1));
    }
    catch(TimeoutException)
    {
    }
  }

  private static int GetConfiguredAddress(AresStruct settings)
  {
    if(settings.Fields.TryGetValue(AddressSettingKey, out var address) && address.HasNumberValue)
      return (int)Math.Round(address.NumberValue);

    return 0;
  }

  private void PublishState()
  {
    _stateSubject.OnNext(
      AresStateBuilder.Create()
        .Add("FirmwareVersion", _currentState.FirmwareVersion)
        .Add("Address", _currentState.Address)
        .Add("DiameterMm", _currentState.DiameterMm)
        .Add("Status", _currentState.Status.ToString())
        .Add("DispensedVolume", _currentState.DispensedVolume)
        .Add("WithdrawnVolume", _currentState.WithdrawnVolume)
        .Add("VolumeUnit", _currentState.VolumeUnits.ToString())
        .Add("PhaseNumber", _currentState.Phase.Number)
        .Add("PhaseFunction", _currentState.Phase.Function.ToString())
        .Add("PhaseRate", _currentState.Phase.Rate)
        .Add("PhaseRateUnit", _currentState.Phase.RateUnit.ToString())
        .Add("PhaseVolume", _currentState.Phase.Volume)
        .Add("PhaseVolumeUnit", _currentState.Phase.VolumeUnit.ToString())
        .Add("PhaseDirection", _currentState.Phase.Direction.ToString())
        .Build());
  }

  private static void EnsureNoError(Response response, string operation)
  {
    if(response.Error is not null)
      throw new InvalidOperationException($"{operation} failed with device error '{response.Error}'.");
  }

  private static async Task<CommandResult> ExecuteNoResult(Func<Task> action)
  {
    await action();
    return new CommandResult { Success = true };
  }

  private static async Task<CommandResult> ExecuteNumeric(AresValue? value, Func<double, Task> action, string commandName, SyringePumpNE1000CommandParameter parameter)
  {
    if(value is not { HasNumberValue: true, NumberValue: var number })
    {
      return new CommandResult
      {
        Success = false,
        Error = $"Command '{commandName}' requires numeric argument '{parameter}'."
      };
    }

    await action(number);
    return new CommandResult { Success = true };
  }

  private async Task<CommandResult> ExecuteFunctionCommand(AresValue? value, string commandName)
  {
    if(value is not { HasStringValue: true, StringValue: var functionName } ||
       !Enum.TryParse<SyringePumpFunction>(functionName, true, out var function))
    {
      return new CommandResult
      {
        Success = false,
        Error = $"Command '{commandName}' requires string argument '{SyringePumpNE1000CommandParameter.Function}' containing a valid NE1000 function token."
      };
    }

    await SetPhaseFunction(function);
    return new CommandResult { Success = true };
  }

  private async Task<CommandResult> ExecuteDirectionCommand(AresValue? value, string commandName)
  {
    if(value is not { HasStringValue: true, StringValue: var directionName } ||
       !Enum.TryParse<Direction>(directionName, true, out var direction))
    {
      return new CommandResult
      {
        Success = false,
        Error = $"Command '{commandName}' requires string argument '{SyringePumpNE1000CommandParameter.Direction}' containing a valid direction token."
      };
    }

    await SetProgramFunctionPumpingDirection(direction);
    return new CommandResult { Success = true };
  }

  private static AresStruct BuildRateStruct(PhaseFunctionRateResponse response)
    => AresStateBuilder.Create()
      .Add("Rate", response.Rate)
      .Add("RateUnit", response.RateUnit.ToString())
      .Add("Status", response.Status.ToString())
      .Build();

  private static AresStruct BuildVolumeStruct(PhaseFunctionVolumeResponse response)
    => AresStateBuilder.Create()
      .Add("Volume", response.Volume)
      .Add("VolumeUnit", response.VolumeUnit.ToString())
      .Add("Status", response.Status.ToString())
      .Build();

  private static AresStruct BuildDispensedVolumeStruct(VolumeDispensedResponse response)
    => AresStateBuilder.Create()
      .Add("Infused", response.Infused)
      .Add("Withdrawn", response.Withdrawn)
      .Add("VolumeUnit", response.SystemVolumeUnit.ToString())
      .Add("Status", response.Status.ToString())
      .Build();
}
