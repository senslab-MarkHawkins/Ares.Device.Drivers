using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Ares.Toolkit.Serial;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UnitsNet;
using TC0304Remastered.Commands;
using TC0304Remastered.Connection;
using TC0304Remastered.Simulation;

namespace TC0304Remastered;

public sealed class DataloggerThermometer : AresDevice, IAsyncDisposable
{
  private const string Probe1NameKey = "Probe1Name";
  private const string Probe2NameKey = "Probe2Name";
  private const string Probe3NameKey = "Probe3Name";
  private const string Probe4NameKey = "Probe4Name";

  private readonly IDataloggerThermometerConnection _connection;
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly IDisposable _stateSubscription;
  private CancellationTokenSource _stateUpdateTokenSource = new();
  private Task _stateUpdater = Task.CompletedTask;
  private bool _connected;
  private bool _disposed;
  private DataResponse? _latestResponse;

  public DataloggerThermometer(DeviceConnectionInfo info) : base(info)
  {
    var serialInfo = info.SerialConnectionInfo ?? throw new InvalidOperationException("TC0304 requires serial connection info.");
    _connection = info.Simulated
      ? new SimDataloggerThermometerConnection(serialInfo.PortName)
      : new DataloggerThermometerConnection(serialInfo.PortName);

    StateStream = _stateSubject.AsObservable();
    _stateSubscription = _connection.GetTransactionStream<DataResponse>()
      .Select(transaction => transaction.Response)
      .Subscribe(UpdateState);

    Type = "TC0304";
    Description = "TC0304 datalogger thermometer driver packaged for ARES plugin loading.";
    HardwareIdentity = "TC0304";
    Version = "1.0.0";

    StateSchema
      .AddEntry("BatteryLow", AresDataType.Boolean, false)
      .AddEntry("Hold", AresDataType.Boolean, false)
      .AddEntry("Celsius", AresDataType.Boolean, false)
      .AddEntry("Mode", AresDataType.String, false)
      .AddEntry("T1T2", AresDataType.Boolean, false)
      .AddEntry("Probe1Name", AresDataType.String, false)
      .AddEntry("Probe2Name", AresDataType.String, false)
      .AddEntry("Probe3Name", AresDataType.String, false)
      .AddEntry("Probe4Name", AresDataType.String, false)
      .AddEntry("T1Probe", AresDataType.Number, true, unit: "C")
      .AddEntry("T2Probe", AresDataType.Number, true, unit: "C")
      .AddEntry("T3Probe", AresDataType.Number, true, unit: "C")
      .AddEntry("T4Probe", AresDataType.Number, true, unit: "C");

    SettingSchema
      .AddEntry(Probe1NameKey, AresDataType.String, true, "Display label for the first probe.")
      .AddEntry(Probe2NameKey, AresDataType.String, true, "Display label for the second probe.")
      .AddEntry(Probe3NameKey, AresDataType.String, true, "Display label for the third probe.")
      .AddEntry(Probe4NameKey, AresDataType.String, true, "Display label for the fourth probe.");

    ApplySettings(info.DeviceSettings);
    PublishState();
  }

  public override IObservable<AresStruct> StateStream { get; }
  public string Probe1Name { get; private set; } = "Probe 1";
  public string Probe2Name { get; private set; } = "Probe 2";
  public string Probe3Name { get; private set; } = "Probe 3";
  public string Probe4Name { get; private set; } = "Probe 4";

  public async Task<DataResponse> GetAndUpdateState()
  {
    await EnsureConnected();
    return await _connection.Send(new DataRequest(), TimeSpan.FromSeconds(5));
  }

  public async Task<double?[]> GetTemperatures()
  {
    var response = await GetAndUpdateState();
    return
    [
      response.T1Probe?.DegreesCelsius,
      response.T2Probe?.DegreesCelsius,
      response.T3Probe?.DegreesCelsius,
      response.T4Probe?.DegreesCelsius
    ];
  }

  public async Task Hold()
  {
    await EnsureConnected();
    await _connection.Send(new HoldCommand());
    if(_latestResponse is not null)
    {
      _latestResponse = new DataResponse
      {
        BatteryLow = _latestResponse.BatteryLow,
        Hold = !_latestResponse.Hold,
        Celsius = _latestResponse.Celsius,
        Mode = _latestResponse.Mode,
        T1T2 = _latestResponse.T1T2,
        T1Probe = _latestResponse.T1Probe,
        T2Probe = _latestResponse.T2Probe,
        T3Probe = _latestResponse.T3Probe,
        T4Probe = _latestResponse.T4Probe
      };
      PublishState();
    }
  }

  public async Task ToggleTemperatureUnit()
  {
    await EnsureConnected();
    await _connection.Send(new ToggleTemperatureUnitCommand());
    await Task.Delay(50);
    try
    {
      await GetAndUpdateState();
    }
    catch(TimeoutException)
    {
    }
  }

  public override async Task<AresStruct> GetState()
  {
    if(_latestResponse is null)
      await GetAndUpdateState();

    return _stateSubject.Value;
  }

  public override Task<AresStruct> GetSettings()
  {
    var settings = AresStructHelper.CreateStringStruct(Probe1NameKey, Probe1Name)
      .AddString(Probe2NameKey, Probe2Name)
      .AddString(Probe3NameKey, Probe3Name)
      .AddString(Probe4NameKey, Probe4Name);

    return Task.FromResult(settings);
  }

  public override Task UpdateSettings(AresStruct settings)
  {
    ApplySettings(settings);
    PublishState();
    return Task.CompletedTask;
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if(!Enum.TryParse<DataLoggerCommand>(command, out var parsedCommand))
    {
      return new CommandResult
      {
        Success = false,
        Error = $"Invalid or unsupported command: '{command}'"
      };
    }

    try
    {
      return parsedCommand switch
      {
        DataLoggerCommand.GetData => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateStruct(BuildDataStruct(await GetAndUpdateState()))
        },
        DataLoggerCommand.GetTemperatures => new CommandResult
        {
          Success = true,
          Result = AresValueHelper.CreateStruct(BuildTemperatureStruct(await GetAndUpdateState()))
        },
        DataLoggerCommand.Hold => await ExecuteNoResult(Hold),
        DataLoggerCommand.ToggleTemperatureUnit => await ExecuteNoResult(ToggleTemperatureUnit),
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

      await GetAndUpdateState();
      await StartStateUpdater(TimeSpan.FromMilliseconds(250));
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = $"TC0304 {Name} is active." };
      return true;
    }
    catch(Exception ex)
    {
      Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = ex.Message };
      return false;
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
    => Task.CompletedTask;

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    var dataSchema = AresSchemaBuilder.Empty()
      .AddEntry("BatteryLow", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
      .AddEntry("Hold", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
      .AddEntry("Celsius", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
      .AddEntry("Mode", AresSchemaBuilder.StringEntry().Build())
      .AddEntry("T1T2", AresSchemaBuilder.Entry(AresDataType.Boolean).Build())
      .AddEntry("T1Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .AddEntry("T2Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .AddEntry("T3Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .AddEntry("T4Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .Build();

    var tempSchema = AresSchemaBuilder.Empty()
      .AddEntry("T1Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .AddEntry("T2Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .AddEntry("T3Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .AddEntry("T4Probe", AresSchemaBuilder.NumberEntry().AsOptional().Build())
      .Build();

    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = DataLoggerCommand.GetData.ToString(),
        Description = "Gets the most recent full data frame from the datalogger.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct).WithStructSchema(dataSchema).Build()
      },
      new()
      {
        Name = DataLoggerCommand.GetTemperatures.ToString(),
        Description = "Gets the most recent temperatures from the datalogger.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.Struct).WithStructSchema(tempSchema).Build()
      },
      new()
      {
        Name = DataLoggerCommand.Hold.ToString(),
        Description = "Toggles hold on the current temperature reading."
      },
      new()
      {
        Name = DataLoggerCommand.ToggleTemperatureUnit.ToString(),
        Description = "Toggles the device display unit between Celsius and Fahrenheit."
      }
    });
  }

  public async ValueTask DisposeAsync()
  {
    if(_disposed)
      return;

    _disposed = true;
    _stateSubscription.Dispose();
    await _stateUpdateTokenSource.CancelAsync();
    try
    {
      await _stateUpdater;
    }
    catch(OperationCanceledException)
    {
    }

    _stateUpdateTokenSource.Dispose();
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

  private static async Task<CommandResult> ExecuteNoResult(Func<Task> action)
  {
    await action();
    return new CommandResult { Success = true };
  }

  private Task EnsureConnected()
  {
    if(_connected)
      return Task.CompletedTask;

    _connection.AttemptOpen();
    _connected = true;
    return Task.CompletedTask;
  }

  private async Task StartStateUpdater(TimeSpan interval)
  {
    await StopStateUpdater();
    _stateUpdateTokenSource.Dispose();
    _stateUpdateTokenSource = new CancellationTokenSource();
    _stateUpdater = Task.Run(async () =>
    {
      while(!_stateUpdateTokenSource.IsCancellationRequested)
      {
        try
        {
          await GetAndUpdateState();
        }
        catch(TimeoutException)
        {
        }

        await Task.Delay(interval, _stateUpdateTokenSource.Token);
      }
    }, _stateUpdateTokenSource.Token);
  }

  private async Task StopStateUpdater()
  {
    _stateUpdateTokenSource.Cancel();
    try
    {
      await _stateUpdater;
    }
    catch(OperationCanceledException)
    {
    }
  }

  private async Task<SerialDeviceValidationResult> Validate()
  {
    try
    {
      await GetAndUpdateState();
      return new SerialDeviceValidationResult(true);
    }
    catch(Exception ex)
    {
      return new SerialDeviceValidationResult(false, ex.Message);
    }
  }

  private void ApplySettings(AresStruct settings)
  {
    Probe1Name = GetSetting(settings, Probe1NameKey, "Probe 1");
    Probe2Name = GetSetting(settings, Probe2NameKey, "Probe 2");
    Probe3Name = GetSetting(settings, Probe3NameKey, "Probe 3");
    Probe4Name = GetSetting(settings, Probe4NameKey, "Probe 4");
  }

  private static string GetSetting(AresStruct settings, string key, string fallback)
  {
    return settings.Fields.TryGetValue(key, out var value) && value.HasStringValue && !string.IsNullOrWhiteSpace(value.StringValue)
      ? value.StringValue
      : fallback;
  }

  private void UpdateState(DataResponse response)
  {
    _latestResponse = response;
    PublishState();
  }

  private void PublishState()
  {
    var state = new AresStruct();
    state.AddBool("BatteryLow", _latestResponse?.BatteryLow ?? false);
    state.AddBool("Hold", _latestResponse?.Hold ?? false);
    state.AddBool("Celsius", _latestResponse?.Celsius ?? true);
    state.AddString("Mode", (_latestResponse?.Mode ?? Commands.Mode.Normal).ToString());
    state.AddBool("T1T2", _latestResponse?.T1T2 ?? false);
    state.AddString("Probe1Name", Probe1Name);
    state.AddString("Probe2Name", Probe2Name);
    state.AddString("Probe3Name", Probe3Name);
    state.AddString("Probe4Name", Probe4Name);

    AddTemperature(state, "T1Probe", _latestResponse?.T1Probe);
    AddTemperature(state, "T2Probe", _latestResponse?.T2Probe);
    AddTemperature(state, "T3Probe", _latestResponse?.T3Probe);
    AddTemperature(state, "T4Probe", _latestResponse?.T4Probe);

    _stateSubject.OnNext(state);
  }

  private static void AddTemperature(AresStruct state, string key, Temperature? temp)
  {
    if(temp.HasValue)
      state.AddNumber(key, temp.Value.DegreesCelsius);
    else
      state.AddNull(key);
  }

  private static AresStruct BuildTemperatureStruct(DataResponse response)
  {
    var state = new AresStruct();
    AddTemperature(state, "T1Probe", response.T1Probe);
    AddTemperature(state, "T2Probe", response.T2Probe);
    AddTemperature(state, "T3Probe", response.T3Probe);
    AddTemperature(state, "T4Probe", response.T4Probe);
    return state;
  }

  private static AresStruct BuildDataStruct(DataResponse response)
  {
    var state = BuildTemperatureStruct(response);
    state.AddBool("BatteryLow", response.BatteryLow);
    state.AddBool("Hold", response.Hold);
    state.AddBool("Celsius", response.Celsius);
    state.AddString("Mode", response.Mode.ToString());
    state.AddBool("T1T2", response.T1T2);
    return state;
  }
}
