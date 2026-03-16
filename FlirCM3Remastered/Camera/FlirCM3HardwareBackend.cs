using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Device;
using SpinnakerNET;

namespace FlirCM3Remastered.Camera;

internal sealed class FlirCM3HardwareBackend : IFlirCM3CameraBackend
{
  private ManagedSystem? _managedSystem;
  private IManagedImageProcessor? _imageProcessor;
  private IManagedCamera? _camera;

  public Task InitializeAsync(CancellationToken cancellationToken)
  {
    _managedSystem ??= new ManagedSystem();
    _imageProcessor ??= new ManagedImageProcessor();

    var cameras = _managedSystem.GetCameras();
    if(cameras.Count == 0)
      throw new InvalidOperationException("No FLIR CM3 camera was detected by Spinnaker.");

    _camera = cameras.First();
    _camera.Init();
    _camera.ExposureAuto.Value = ExposureAutoEnums.Off.ToString();
    _camera.ExposureTime.Value = _camera.ExposureTime.Max;
    _camera.GainAuto.Value = GainAutoEnums.Off.ToString();
    _camera.AcquisitionMode.Value = AcquisitionModeEnums.SingleFrame.ToString();
    _camera.PixelFormat.Value = PixelFormatEnums.RGB8.ToString();
    _camera.BalanceWhiteAuto.Value = BalanceWhiteAutoEnums.Off.ToString();
    return Task.CompletedTask;
  }

  public Task ApplySettingsAsync(FlirCM3Settings settings, CancellationToken cancellationToken)
  {
    EnsureInitialized();
    var camera = _camera!;
    camera.Width.Value = settings.CaptureWidth;
    camera.Height.Value = settings.CaptureHeight;
    camera.OffsetX.Value = settings.OffsetX;
    camera.OffsetY.Value = settings.OffsetY;
    camera.ExposureTime.Value = settings.ExposureTime;
    camera.Gain.Value = settings.Gain;
    camera.BlackLevel.Value = settings.BlackLevel;
    camera.PixelFormat.Value = PixelFormatEnums.RGB8.ToString();

    camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Blue.ToString();
    camera.BalanceRatio.Value = settings.BlueBalance;

    camera.BalanceRatioSelector.Value = BalanceRatioSelectorEnums.Red.ToString();
    camera.BalanceRatio.Value = settings.RedBalance;
    return Task.CompletedTask;
  }

  public Task SetExposureTimeAsync(double desiredExposureTime, CancellationToken cancellationToken)
  {
    EnsureInitialized();
    var camera = _camera!;
    if(camera.ExposureTime is null || !camera.ExposureTime.IsWritable || !camera.ExposureTime.IsReadable)
      return Task.CompletedTask;

    camera.ExposureTime.Value = desiredExposureTime > camera.ExposureTime.Max ? camera.ExposureTime.Max : desiredExposureTime;
    return Task.CompletedTask;
  }

  public async Task<CapturedImageResult> CaptureImageAsync(string basePath, CancellationToken cancellationToken)
  {
    EnsureInitialized();
    var camera = _camera!;
    var imageProcessor = _imageProcessor!;
    var outputDirectory = string.IsNullOrWhiteSpace(basePath)
      ? Path.Combine(Path.GetTempPath(), "ARES", "FlirCM3")
      : basePath;

    Directory.CreateDirectory(outputDirectory);

    if(camera.IsStreaming())
      camera.EndAcquisition();

    var timeout = camera.ExposureTime.Value / 1000d + 1000d;
    camera.BeginAcquisition();
    var rawImage = camera.GetNextImage((ulong)timeout);

    if(rawImage.IsIncomplete)
      Console.WriteLine($"Image incomplete with image status {rawImage.ImageStatus}");

    var convertedImage = imageProcessor.Convert(rawImage, PixelFormatEnums.RGB8);

    var savePath = Path.Combine(outputDirectory, "sample_image.tif");
    var pngPath = Path.Combine(outputDirectory, "sample_image.png");

    convertedImage.Save(savePath);
    convertedImage.Save(pngPath);

    var imageData = await File.ReadAllBytesAsync(savePath, cancellationToken);
    var displayImageData = await File.ReadAllBytesAsync(pngPath, cancellationToken);

    if(File.Exists(pngPath))
      File.Delete(pngPath);

    camera.EndAcquisition();

    return new CapturedImageResult
    {
      ImageData = imageData,
      DisplayImageData = displayImageData,
      LatestImagePath = savePath
    };
  }

  public Task<AresStruct> GetStateAsync(CancellationToken cancellationToken)
  {
    EnsureInitialized();
    var camera = _camera!;
    return Task.FromResult(
      AresStateBuilder.Create()
        .Add("Width", camera.Width.Value)
        .Add("Height", camera.Height.Value)
        .Add("OffsetX", camera.OffsetX.Value)
        .Add("OffsetY", camera.OffsetY.Value)
        .Add("Exposure Time", camera.ExposureTime.Value)
        .Add("Gain", camera.Gain.Value)
        .Add("Black Level", camera.BlackLevel.Value)
        .Add("Pixel Format", camera.PixelFormat.Value.ToString())
        .Build());
  }

  public ValueTask DisposeAsync()
  {
    _camera?.Dispose();
    _imageProcessor?.Dispose();
    _managedSystem?.Dispose();
    return ValueTask.CompletedTask;
  }

  private void EnsureInitialized()
  {
    if(_camera is null || _imageProcessor is null || _managedSystem is null)
      throw new InvalidOperationException("FLIR CM3 camera backend has not been initialized.");
  }
}
