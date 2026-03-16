using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;

namespace LaserChillerRemastered.UI;

public partial class LaserChillerUnitControlViewModel : DeviceUnitControlViewModel<LaserChiller>, IAsyncDisposable
{
  private readonly IDisposable _stateSubscription;

  public LaserChillerUnitControlViewModel(LaserChiller device) : base(device)
  {
    ViewType = typeof(LaserChillerUnitControl);
    DefaultWidth = 16;

    _stateSubscription = device.StateStream.Subscribe(state =>
    {
      if(state.Fields.TryGetValue("CurrentTemperature", out var currentTemperature) && currentTemperature.HasNumberValue)
        CurrentTemperature = currentTemperature.NumberValue;

      if(state.Fields.TryGetValue("TargetTemperature", out var targetTemperature) && targetTemperature.HasNumberValue)
      {
        TargetTemperature = targetTemperature.NumberValue;
        if(!DesiredTemperature.HasValue)
          DesiredTemperature = targetTemperature.NumberValue;
      }

      if(state.Fields.TryGetValue("Mode", out var mode) && mode.HasStringValue)
        Mode = mode.StringValue;

      HasValidData = true;
    });

    RunModeCommand = ReactiveCommand.CreateFromTask(Device.SetChillerRunMode);
    StandbyModeCommand = ReactiveCommand.CreateFromTask(Device.SetChillerStandbyMode);
    RefreshTemperatureCommand = ReactiveCommand.CreateFromTask(Device.GetManifoldTemperature);
  }

  [Reactive]
  public partial double CurrentTemperature { get; private set; }

  [Reactive]
  public partial double TargetTemperature { get; private set; }

  [Reactive]
  public partial double? DesiredTemperature { get; set; }

  [Reactive]
  public partial string Mode { get; private set; } = "Unknown";

  [Reactive]
  public partial bool HasValidData { get; private set; }

  public ReactiveCommand<Unit, Unit> RunModeCommand { get; }
  public ReactiveCommand<Unit, Unit> StandbyModeCommand { get; }
  public ReactiveCommand<Unit, double?> RefreshTemperatureCommand { get; }

  public async Task ApplyTargetTemperature()
  {
    if(!DesiredTemperature.HasValue)
      return;

    await Device.SetStabilizedTemperature(DesiredTemperature.Value);
  }

  public ValueTask DisposeAsync()
  {
    _stateSubscription.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
