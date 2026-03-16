using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;

namespace FlirCM3Remastered.Camera;

internal sealed class SimFlirCM3Backend : IFlirCM3CameraBackend
{
  private readonly string _simulationImagePath;
  private readonly string _simulationPreviewPath;
  private FlirCM3Settings _settings = new();

  public SimFlirCM3Backend(string simulationImagePath, string simulationPreviewPath)
  {
    _simulationImagePath = simulationImagePath;
    _simulationPreviewPath = simulationPreviewPath;
  }

  public Task InitializeAsync(CancellationToken cancellationToken)
    => Task.CompletedTask;

  public Task ApplySettingsAsync(FlirCM3Settings settings, CancellationToken cancellationToken)
  {
    _settings = settings;
    return Task.CompletedTask;
  }

  public Task SetExposureTimeAsync(double desiredExposureTime, CancellationToken cancellationToken)
  {
    _settings.ExposureTime = desiredExposureTime;
    return Task.CompletedTask;
  }

  public async Task<CapturedImageResult> CaptureImageAsync(string basePath, CancellationToken cancellationToken)
  {
    var outputDirectory = string.IsNullOrWhiteSpace(basePath)
      ? Path.Combine(Path.GetTempPath(), "ARES", "FlirCM3")
      : basePath;

    var sourcePath = _simulationImagePath;
    Directory.CreateDirectory(outputDirectory);

    var savePath = Path.Combine(outputDirectory, "sample_image.tif");
    File.Copy(sourcePath, savePath, true);

    var bytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
    var displayBytes = await File.ReadAllBytesAsync(_simulationPreviewPath, cancellationToken);
    return new CapturedImageResult
    {
      ImageData = bytes,
      DisplayImageData = displayBytes,
      LatestImagePath = savePath
    };
  }

  public Task<AresStruct> GetStateAsync(CancellationToken cancellationToken)
  {
    return Task.FromResult(
      AresStateBuilder.Create()
        .Add("Width", _settings.CaptureWidth)
        .Add("Height", _settings.CaptureHeight)
        .Add("OffsetX", _settings.OffsetX)
        .Add("OffsetY", _settings.OffsetY)
        .Add("Exposure Time", _settings.ExposureTime)
        .Add("Gain", _settings.Gain)
        .Add("Black Level", _settings.BlackLevel)
        .Add("Pixel Format", "Simulated Camera, no format")
        .Build());
  }

  public ValueTask DisposeAsync()
    => ValueTask.CompletedTask;
}
