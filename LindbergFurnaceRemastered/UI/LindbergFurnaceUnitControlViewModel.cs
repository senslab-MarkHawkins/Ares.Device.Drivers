using Ares.Toolkit.Device.UI;
using LindbergFurnaceRemastered.UI.State;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using UnitsNet;
using UnitsNet.Units;

namespace LindbergFurnaceRemastered.UI;

public partial class LindbergFurnaceUnitControlViewModel : DeviceUnitControlViewModel<LindbergTubeFurnace>, IAsyncDisposable
{
  private IDisposable? _stateSubscription;
  private readonly ILogger<LindbergFurnaceUnitControlViewModel> _logger;
  private TemperatureUnit _temperatureUnit = TemperatureUnit.DegreeCelsius;

  public LindbergFurnaceUnitControlViewModel(LindbergTubeFurnace furnace, ILogger<LindbergFurnaceUnitControlViewModel> logger) : base(furnace)
  {
    _logger = logger;
    FurnaceState = new LindbergFurnaceState();
    _stateSubscription = furnace.StateStream
      .Select(LindbergFurnaceStateMapper.FromAresStruct)
      .Subscribe(newState =>
      {
        FurnaceState = newState;
        UpdateUnits();
        HasValidData = true;
      });

    ViewType = typeof(LindbergFurnaceUnitControl);
    DefaultWidth = 17;
  }

  private void UpdateUnits()
  {
    CurrentTemperatureValue = Temperature.FromDegreesCelsius(FurnaceState.CurrentTemperature).As(TemperatureUnit);
    TargetTemperatureValue = Temperature.FromDegreesCelsius(FurnaceState.Setpoint).As(TemperatureUnit);
  }

  public async ValueTask DisposeAsync()
  {
    _stateSubscription?.Dispose();
    GC.SuppressFinalize(this);
  }

  public async Task SetTargetTemperature()
  {
    if(!TargetTemperatureValue.HasValue)
      return;

    var temp = Temperature.From(TargetTemperatureValue.Value, TemperatureUnit);
    await Device.SetSetpointInternal(temp.DegreesCelsius);
  }

  public TemperatureUnit TemperatureUnit
  {
    get => _temperatureUnit;
    set
    {
      this.RaiseAndSetIfChanged(ref _temperatureUnit, value);
      UpdateUnits();
    }
  }

  [Reactive]
  public partial double? CurrentTemperatureValue { get; private set; }
  [Reactive]
  public partial double? TargetTemperatureValue { get; set; }
  [Reactive]
  public partial bool HasValidData { get; private set; }
  [Reactive]
  public partial LindbergFurnaceState FurnaceState { get; set; }
}
