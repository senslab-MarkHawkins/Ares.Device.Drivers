using Ares.Datamodel;
using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using TC0304Remastered.Commands;

namespace TC0304Remastered.UI;

public partial class Tc0304UnitControlViewModel : DeviceUnitControlViewModel<DataloggerThermometer>, IAsyncDisposable
{
  private readonly IDisposable _stateSubscription;

  public Tc0304UnitControlViewModel(DataloggerThermometer device) : base(device)
  {
    ViewType = typeof(Tc0304ControlWidgetView);
    DefaultWidth = 22;

    _stateSubscription = device.StateStream.Subscribe(state =>
    {
      CurrentState = new Tc0304State
      {
        BatteryLow = state.Fields.TryGetValue("BatteryLow", out var batteryLow) && batteryLow.HasBoolValue && batteryLow.BoolValue,
        Hold = state.Fields.TryGetValue("Hold", out var hold) && hold.HasBoolValue && hold.BoolValue,
        Celsius = !state.Fields.TryGetValue("Celsius", out var celsius) || !celsius.HasBoolValue || celsius.BoolValue,
        Mode = state.Fields.TryGetValue("Mode", out var mode) && mode.HasStringValue ? mode.StringValue : "Normal",
        Probe1Name = GetString(state, "Probe1Name", "Probe 1"),
        Probe2Name = GetString(state, "Probe2Name", "Probe 2"),
        Probe3Name = GetString(state, "Probe3Name", "Probe 3"),
        Probe4Name = GetString(state, "Probe4Name", "Probe 4"),
        T1Probe = GetOptionalNumber(state, "T1Probe"),
        T2Probe = GetOptionalNumber(state, "T2Probe"),
        T3Probe = GetOptionalNumber(state, "T3Probe"),
        T4Probe = GetOptionalNumber(state, "T4Probe")
      };
      HasValidData = true;
    });

    HoldCommand = ReactiveCommand.CreateFromTask(Device.Hold);
    ToggleTemperatureUnitCommand = ReactiveCommand.CreateFromTask(Device.ToggleTemperatureUnit);
    RefreshCommand = ReactiveCommand.CreateFromTask(Device.GetAndUpdateState);
  }

  [Reactive]
  public partial Tc0304State CurrentState { get; private set; } = new();

  [Reactive]
  public partial bool HasValidData { get; private set; }

  public ReactiveCommand<Unit, Unit> HoldCommand { get; }
  public ReactiveCommand<Unit, Unit> ToggleTemperatureUnitCommand { get; }
  public ReactiveCommand<Unit, DataResponse> RefreshCommand { get; }

  public ValueTask DisposeAsync()
  {
    _stateSubscription.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }

  private static string GetString(AresStruct state, string key, string fallback)
  {
    return state.Fields.TryGetValue(key, out var value) && value.HasStringValue ? value.StringValue : fallback;
  }

  private static double? GetOptionalNumber(AresStruct state, string key)
  {
    return state.Fields.TryGetValue(key, out var value) && value.HasNumberValue ? value.NumberValue : null;
  }
}
