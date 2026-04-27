using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using VacuumPumpPlugin.UI.State;

namespace VacuumPumpPlugin.UI;

public partial class VacuumUnitControlViewModel : DeviceUnitControlViewModel<VacuumPump>
{
  private IDisposable? _stateSubscription;

  public VacuumUnitControlViewModel(VacuumPump pump) : base(pump)
  {
    VacuumState = new VacuumState();
    _stateSubscription = pump.StateStream
      .Select(VacuumStateMapper.FromAresStruct)
      .Subscribe(newState =>
      {
        VacuumState = newState;
        HasValidData = true;
      });

    ViewType = typeof(VacuumUnitControl);
    DefaultWidth = 12;
  }

  [Reactive]
  public partial VacuumState VacuumState { get; set; }

  [Reactive]
  public partial bool HasValidData { get; private set; }
}
