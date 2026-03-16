using Ares.Datamodel;
using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace FlirCM3Remastered.UI;

public partial class FlirCM3UnitControlViewModel : DeviceUnitControlViewModel<FlirCM3Camera>, IAsyncDisposable
{
  private readonly IDisposable _stateSubscription;

  public FlirCM3UnitControlViewModel(FlirCM3Camera device) : base(device)
  {
    ViewType = typeof(FlirCM3ControlWidgetView);
    DefaultWidth = 30;

    _stateSubscription = device.StateStream.Subscribe(state =>
    {
      if(state.Fields.TryGetValue("Exposure Time", out var exposureTime) && exposureTime.HasNumberValue)
        ExposureTime = exposureTime.NumberValue;

      HasState = true;
    });
  }

  [Reactive]
  public partial double ExposureTime { get; set; }

  [Reactive]
  public partial byte[]? ImageData { get; private set; }

  [Reactive]
  public partial byte[]? DisplayData { get; private set; }

  [Reactive]
  public partial bool HasState { get; private set; }

  [Reactive]
  public partial string? LastError { get; set; }

  [Reactive]
  public partial string? LastCapturePath { get; private set; }

  public async Task CaptureImage()
  {
    LastError = null;
    ImageData = await Device.CaptureImage(string.Empty);
    DisplayData = Device.DisplayImageData;
    LastCapturePath = Device.LatestImagePath;
  }

  public async Task ApplyExposureTime()
  {
    LastError = null;
    await Device.SetExposureTime(ExposureTime);
  }

  public ValueTask DisposeAsync()
  {
    _stateSubscription.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
