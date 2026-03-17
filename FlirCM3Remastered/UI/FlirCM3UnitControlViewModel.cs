using Ares.Toolkit.Device.UI;
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

  public async Task SetExposureTime() => await Device.SetExposureTime(ExposureTime);

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
    try
    {
      LastError = null;
      ImageData = await Device.CaptureImage(string.Empty);
      DisplayData = Device.DisplayImageData;
      LastCapturePath = Device.LatestImagePath;
    }

    catch(Exception)
    {
      LastError = $"Failed to capture camera image";
    }
  }

  public ValueTask DisposeAsync()
  {
    _stateSubscription.Dispose();
    GC.SuppressFinalize(this);
    return ValueTask.CompletedTask;
  }
}
