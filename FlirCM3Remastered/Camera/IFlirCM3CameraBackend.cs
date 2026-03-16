using Ares.Datamodel;

namespace FlirCM3Remastered.Camera;

internal interface IFlirCM3CameraBackend : IAsyncDisposable
{
  Task InitializeAsync(CancellationToken cancellationToken);
  Task ApplySettingsAsync(FlirCM3Settings settings, CancellationToken cancellationToken);
  Task SetExposureTimeAsync(double desiredExposureTime, CancellationToken cancellationToken);
  Task<CapturedImageResult> CaptureImageAsync(string basePath, CancellationToken cancellationToken);
  Task<AresStruct> GetStateAsync(CancellationToken cancellationToken);
}
