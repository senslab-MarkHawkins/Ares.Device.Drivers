using Ares.Datamodel;
using Ares.Toolkit.Device.UI;
using ReactiveUI.SourceGenerators;

namespace SyringePumpNE1000Remastered.UI;

public partial class SyringePumpUnitControlViewModel : DeviceUnitControlViewModel<SyringePumpNE1000Device>, IAsyncDisposable
{
  private readonly IDisposable _stateSubscription;

  public SyringePumpUnitControlViewModel(SyringePumpNE1000Device device) : base(device)
  {
    ViewType = typeof(SyringePumpUnitControl);
    DefaultWidth = 30;
    AvailableFunctions = Enum.GetValues<SyringePumpFunction>().Where(value => value != SyringePumpFunction.UndefinedFunction).ToArray();
    AvailableDirections = Enum.GetValues<Direction>().Where(value => value != Direction.UndefinedDirection).ToArray();

    _stateSubscription = device.StateStream.Subscribe(UpdateState);
  }

  public IReadOnlyList<SyringePumpFunction> AvailableFunctions { get; }
  public IReadOnlyList<Direction> AvailableDirections { get; }

  [Reactive]
  public partial bool HasValidData { get; private set; }

  [Reactive]
  public partial string FirmwareVersion { get; private set; } = string.Empty;

  [Reactive]
  public partial int Address { get; private set; }

  [Reactive]
  public partial double DiameterMm { get; private set; }

  [Reactive]
  public partial double DispensedVolume { get; private set; }

  [Reactive]
  public partial double WithdrawnVolume { get; private set; }

  [Reactive]
  public partial string VolumeUnit { get; private set; } = "Ml";

  [Reactive]
  public partial int PhaseNumber { get; private set; }

  [Reactive]
  public partial SyringePumpFunction PhaseFunction { get; private set; } = SyringePumpFunction.UndefinedFunction;

  [Reactive]
  public partial double PhaseRate { get; private set; }

  [Reactive]
  public partial string PhaseRateUnit { get; private set; } = "Mm";

  [Reactive]
  public partial double PhaseVolume { get; private set; }

  [Reactive]
  public partial string PhaseVolumeUnit { get; private set; } = "Ml";

  [Reactive]
  public partial Direction PhaseDirection { get; private set; } = Direction.Inf;

  [Reactive]
  public partial string StatusText { get; private set; } = "Waiting for state";

  [Reactive]
  public partial int TargetAddress { get; set; }

  [Reactive]
  public partial double TargetDiameterMm { get; set; }

  [Reactive]
  public partial int TargetPhase { get; set; }

  [Reactive]
  public partial SyringePumpFunction TargetFunction { get; set; } = SyringePumpFunction.Rat;

  [Reactive]
  public partial double TargetRateMlPerMin { get; set; }

  [Reactive]
  public partial double TargetVolumeMl { get; set; }

  [Reactive]
  public partial Direction TargetDirection { get; set; } = Direction.Inf;

  public Task Refresh()
    => Device.RefreshState();

  public Task ApplyAddress()
    => Device.SetAddress(TargetAddress);

  public Task ApplyDiameter()
    => Device.SetDiameter(TargetDiameterMm);

  public Task ApplyPhase()
    => Device.SetPhase(TargetPhase);

  public Task ApplyFunction()
    => Device.SetPhaseFunction(TargetFunction);

  public Task ApplyRate()
    => Device.SetProgramFunctionRate(TargetRateMlPerMin);

  public Task ApplyVolume()
    => Device.SetProgramFunctionVolumeToBeDispensed(TargetVolumeMl);

  public Task ApplyDirection()
    => Device.SetProgramFunctionPumpingDirection(TargetDirection);

  public Task Purge()
    => Device.PurgePump();

  public Task Start()
    => Device.StartPumpingProgram();

  public Task Stop()
    => Device.StopPumpingProgram();

  public Task ClearVolume()
    => Device.ClearVolumeDispensed(PhaseDirection);

  public ValueTask DisposeAsync()
  {
    _stateSubscription.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }

  private void UpdateState(AresStruct state)
  {
    FirmwareVersion = GetString(state, "FirmwareVersion", string.Empty);
    Address = (int)GetNumber(state, "Address");
    DiameterMm = GetNumber(state, "DiameterMm");
    DispensedVolume = GetNumber(state, "DispensedVolume");
    WithdrawnVolume = GetNumber(state, "WithdrawnVolume");
    VolumeUnit = GetString(state, "VolumeUnit", "Ml");
    PhaseNumber = (int)GetNumber(state, "PhaseNumber");
    PhaseFunction = Enum.TryParse<SyringePumpFunction>(GetString(state, "PhaseFunction", SyringePumpFunction.UndefinedFunction.ToString()), out var function)
      ? function
      : SyringePumpFunction.UndefinedFunction;
    PhaseRate = GetNumber(state, "PhaseRate");
    PhaseRateUnit = GetString(state, "PhaseRateUnit", "Mm");
    PhaseVolume = GetNumber(state, "PhaseVolume");
    PhaseVolumeUnit = GetString(state, "PhaseVolumeUnit", "Ml");
    PhaseDirection = Enum.TryParse<Direction>(GetString(state, "PhaseDirection", Direction.Inf.ToString()), out var direction)
      ? direction
      : Direction.Inf;

    TargetAddress = Address;
    TargetDiameterMm = DiameterMm;
    TargetPhase = PhaseNumber;
    TargetFunction = PhaseFunction == SyringePumpFunction.UndefinedFunction ? SyringePumpFunction.Rat : PhaseFunction;
    TargetRateMlPerMin = PhaseRate;
    TargetVolumeMl = PhaseVolume;
    TargetDirection = PhaseDirection;

    StatusText = GetStatusText(GetString(state, "Status", StatusPrompt.UndefinedStatusPrompt.ToString()), PhaseRate);
    HasValidData = true;
  }

  private static string GetString(AresStruct state, string key, string fallback)
    => state.Fields.TryGetValue(key, out var value) && value.HasStringValue ? value.StringValue : fallback;

  private static double GetNumber(AresStruct state, string key)
    => state.Fields.TryGetValue(key, out var value) && value.HasNumberValue ? value.NumberValue : 0;

  private static string GetStatusText(string statusName, double rate)
  {
    if(!Enum.TryParse<StatusPrompt>(statusName, out var status))
      return statusName;

    return status switch
    {
      StatusPrompt.PromptI => $"Infusing {rate:F3} mL/min",
      StatusPrompt.PromptW => $"Withdrawing {rate:F3} mL/min",
      StatusPrompt.PromptS => "Stopped",
      StatusPrompt.PromptP => "Paused",
      StatusPrompt.PromptT => "Timed pause",
      StatusPrompt.PromptU => "Trigger wait",
      StatusPrompt.PromptX => "Purging",
      _ => "Unknown"
    };
  }
}
