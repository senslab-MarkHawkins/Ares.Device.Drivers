using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using FlirCM3Remastered.Camera;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;

namespace FlirCM3Remastered;

public sealed class FlirCM3Camera : AresDevice, IAsyncDisposable
{
  private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
  private readonly IFlirCM3CameraBackend _backend;
  private readonly string _simulationImagePath;
  private readonly string _simulationPreviewPath;
  private bool _activated;
  private bool _disposed;

  public FlirCM3Camera(DeviceConnectionInfo info) : base(info)
  {
    var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
      ?? AppContext.BaseDirectory;

    _simulationImagePath = Path.Combine(assemblyDirectory, "Simulation", "test_result.tiff");
    _simulationPreviewPath = Path.Combine(assemblyDirectory, "Simulation", "test_baseline.png");
    _backend = info.Simulated
      ? new SimFlirCM3Backend(_simulationImagePath, _simulationPreviewPath)
      : new FlirCM3HardwareBackend();

    StateStream = _stateSubject.AsObservable();
    Settings = FlirCM3Settings.FromStruct(info.DeviceSettings);

    Type = "Flir CM3 Camera";
    Description = "FLIR CM3 packaged camera driver for ARES plugin loading.";
    HardwareIdentity = "FlirCM3";
    Version = "1.0.0";

    StateSchema
      .AddEntry("Width", AresDataType.Number, false)
      .AddEntry("Height", AresDataType.Number, false)
      .AddEntry("OffsetX", AresDataType.Number, false)
      .AddEntry("OffsetY", AresDataType.Number, false)
      .AddEntry("Exposure Time", AresDataType.Number, false)
      .AddEntry("Gain", AresDataType.Number, false)
      .AddEntry("Black Level", AresDataType.Number, false)
      .AddEntry("Pixel Format", AresDataType.String, false);

    SettingSchema
      .AddEntry(FlirCM3Settings.ExposureCompensationKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.ExposureTimeKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.GainKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.GammaKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.BlackLevelKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.RedBalanceKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.BlueBalanceKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.CaptureWidthKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.CaptureHeightKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.OffsetXKey, AresDataType.Number, false)
      .AddEntry(FlirCM3Settings.OffsetYKey, AresDataType.Number, false);
  }

  public override IObservable<AresStruct> StateStream { get; }
  public FlirCM3Settings Settings { get; private set; }
  public byte[] ImageData { get; private set; } = Array.Empty<byte>();
  public byte[] DisplayImageData { get; private set; } = Array.Empty<byte>();
  public string LatestImagePath { get; private set; } = string.Empty;

  public override async Task<bool> Activate(CancellationToken ct)
  {
    try
    {
      await _backend.InitializeAsync(ct);
      await _backend.ApplySettingsAsync(Settings, ct);
      await PublishState(ct);
      _activated = true;
      Status = new DeviceOperationalStatus
      {
        OperationalState = OperationalState.Active,
        Message = "Activated Flir CM3 Camera"
      };
      return true;
    }
    catch(Exception ex)
    {
      Status = new DeviceOperationalStatus
      {
        OperationalState = OperationalState.Error,
        Message = ex.Message
      };
      return false;
    }
  }

  public override Task EnterSafeMode(CancellationToken ct)
    => Task.CompletedTask;

  public override Task<AresStruct> GetState()
    => Task.FromResult(_stateSubject.Value);

  public override Task<AresStruct> GetSettings()
    => Task.FromResult(Settings.ToStruct());

  public override async Task UpdateSettings(AresStruct settings)
  {
    Settings = FlirCM3Settings.FromStruct(settings);

    if(_activated)
    {
      await _backend.ApplySettingsAsync(Settings, CancellationToken.None);
      await PublishState(CancellationToken.None);
    }
  }

  public async Task SetExposureTime(double desiredExposureTime, CancellationToken cancellationToken = default)
  {
    await _backend.SetExposureTimeAsync(desiredExposureTime, cancellationToken);
    Settings.ExposureTime = desiredExposureTime;
    await PublishState(cancellationToken);
  }

  public async Task<byte[]> CaptureImage(string basePath, CancellationToken cancellationToken = default)
  {
    var normalizedBasePath = basePath ?? string.Empty;
    var capture = await _backend.CaptureImageAsync(normalizedBasePath, cancellationToken);
    ImageData = capture.ImageData;
    DisplayImageData = capture.DisplayImageData;
    LatestImagePath = capture.LatestImagePath;
    return capture.ImageData;
  }

  public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
  {
    if(!Enum.TryParse<FlirCM3Command>(command, out var parsedCommand))
    {
      return new CommandResult
      {
        Success = false,
        Error = $"Invalid or unsupported command: '{command}'"
      };
    }

    try
    {
      switch(parsedCommand)
      {
        case FlirCM3Command.CaptureImage:
          var savePath = arguments.FirstOrDefault(a => a.ArgName == FlirCM3CommandParameter.SavePath.ToString())?.ArgValue;
          var capturePath = savePath is { HasStringValue: true } ? savePath.StringValue : string.Empty;
          var imageData = await CaptureImage(capturePath, token);
          return new CommandResult { Success = true, Result = AresValueHelper.CreateBytes(imageData) };

        case FlirCM3Command.SetExposureTime:
          var exposureTime = arguments.FirstOrDefault(a => a.ArgName == FlirCM3CommandParameter.ExposureTime.ToString())?.ArgValue;
          if(exposureTime is not { HasNumberValue: true })
          {
            return new CommandResult
            {
              Success = false,
              Error = $"Command '{command}' requires numeric argument '{FlirCM3CommandParameter.ExposureTime}'."
            };
          }

          await SetExposureTime(exposureTime.NumberValue, token);
          return new CommandResult { Success = true };

        case FlirCM3Command.GetLatestImage:
          return new CommandResult { Success = true, Result = AresValueHelper.CreateBytes(ImageData) };

        case FlirCM3Command.GetDisplayImage:
          return new CommandResult { Success = true, Result = AresValueHelper.CreateBytes(DisplayImageData) };

        case FlirCM3Command.GetLatestImagePath:
          return new CommandResult { Success = true, Result = AresValueHelper.CreateString(LatestImagePath) };

        default:
          return new CommandResult
          {
            Success = false,
            Error = $"Execution logic is missing for '{command}'."
          };
      }
    }
    catch(Exception ex)
    {
      return new CommandResult
      {
        Success = false,
        Error = ex.Message
      };
    }
  }

  protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
  {
    return Task.FromResult(new List<DeviceCommandDescriptor>
    {
      new()
      {
        Name = FlirCM3Command.CaptureImage.ToString(),
        Description = "Captures a single image and returns the TIFF bytes.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(
            FlirCM3CommandParameter.SavePath.ToString(),
            AresSchemaBuilder.StringEntry()
              .AsOptional()
              .WithDescription("Optional base path for the saved image files.")
              .Build())
          .Build(),
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.ByteArray)
          .WithDescription("Captured image data in TIFF format.")
          .Build()
      },
      new()
      {
        Name = FlirCM3Command.SetExposureTime.ToString(),
        Description = "Sets the camera exposure time in microseconds.",
        InputSchema = AresSchemaBuilder.Empty()
          .AddEntry(
            FlirCM3CommandParameter.ExposureTime.ToString(),
            AresSchemaBuilder.NumberEntry().WithDescription("Desired exposure time in microseconds.").Build())
          .Build()
      },
      new()
      {
        Name = FlirCM3Command.GetLatestImage.ToString(),
        Description = "Returns the latest captured TIFF image bytes.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.ByteArray).Build()
      },
      new()
      {
        Name = FlirCM3Command.GetDisplayImage.ToString(),
        Description = "Returns the latest preview image bytes.",
        OutputSchema = AresSchemaBuilder.Entry(AresDataType.ByteArray).Build()
      },
      new()
      {
        Name = FlirCM3Command.GetLatestImagePath.ToString(),
        Description = "Returns the file path to the latest captured image.",
        OutputSchema = AresSchemaBuilder.StringEntry().Build()
      }
    });
  }

  public async ValueTask DisposeAsync()
  {
    if(_disposed)
      return;

    _disposed = true;
    await _backend.DisposeAsync();
    _stateSubject.OnCompleted();
    _stateSubject.Dispose();
  }

  protected override void Dispose(bool disposing)
  {
    if(disposing && !_disposed)
      DisposeAsync().AsTask().GetAwaiter().GetResult();

    base.Dispose(disposing);
  }

  private async Task PublishState(CancellationToken cancellationToken)
  {
    var state = await _backend.GetStateAsync(cancellationToken);
    _stateSubject.OnNext(state);
  }
}
