using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Device;
using Microsoft.Extensions.Logging;
using System.Drawing.Imaging;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UEyeCamera.Backend;

namespace UEyeCamera;

public enum UEyeCameraCommand
{
    CaptureImage,
    ApplySettings,
    GetStatus
}

public class UEyeCameraDevice : AresDevice, IAsyncDisposable
{
    private readonly BehaviorSubject<AresStruct> _stateSubject = new(new AresStruct());
    private readonly IUEyeCameraBackend _backend;
    private readonly ILogger _logger;
    private CancellationTokenSource _pollingCts = new();
    private Task _pollingTask = Task.CompletedTask;
    private UEyeCameraSettings _settings;

    public UEyeCameraDevice(DeviceConnectionInfo connectionInfo, ILogger logger) : base(connectionInfo)
    {
        _logger = logger;
        _settings = UEyeCameraSettings.FromStruct(connectionInfo.DeviceSettings);
        
        _backend = connectionInfo.Simulated 
            ? new SimUEyeCameraBackend() 
            : new UEyeCameraHardwareBackend();

        StateStream = _stateSubject.AsObservable();

        Type = "IDS uEye Camera";
        Description = "Remastered uEye camera driver.";
        HardwareIdentity = "uEye";
        Version = "1.0.0";

        StateSchema
            .AddEntry("FPS", AresDataType.Number, false)
            .AddEntry("CaptureStatus", AresDataType.Number, false)
            .AddEntry("IsOpened", AresDataType.Boolean, false);

        SettingSchema
            .AddEntry(UEyeCameraSettings.GainKey, AresDataType.Number, false)
            .AddEntry(UEyeCameraSettings.ExposureKey, AresDataType.Number, false)
            .AddEntry(UEyeCameraSettings.PixelClockKey, AresDataType.Number, false)
            .AddEntry(UEyeCameraSettings.AutoWhiteBalanceKey, AresDataType.Boolean, false)
            .AddEntry(UEyeCameraSettings.AutoGainKey, AresDataType.Boolean, false);
    }

    public override IObservable<AresStruct> StateStream { get; }

    public override async Task<bool> Activate(CancellationToken ct)
    {
        try
        {
            await _backend.InitializeAsync(_settings.CameraId, ct);
            await _backend.ApplySettingsAsync(_settings, ct);
            
            await StartPolling();

            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Active, Message = "uEye Camera activated." };
            return true;
        }
        catch (Exception ex)
        {
            Status = new DeviceOperationalStatus { OperationalState = OperationalState.Error, Message = ex.Message };
            return false;
        }
    }

    public override Task EnterSafeMode(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public override async Task<AresStruct> GetState()
    {
        return _stateSubject.Value;
    }

    private async Task StartPolling()
    {
        _pollingCts.Cancel();
        await _pollingTask;
        _pollingCts = new CancellationTokenSource();

        _pollingTask = Task.Run(async () =>
        {
            while (!_pollingCts.Token.IsCancellationRequested)
            {
                try
                {
                    var fps = await _backend.GetFPSAsync(_pollingCts.Token);
                    var status = await _backend.GetCaptureStatusAsync(_pollingCts.Token);

                    var nextState = AresStateBuilder.Create()
                        .Add("FPS", fps)
                        .Add("CaptureStatus", status)
                        .Add("IsOpened", _backend.IsOpened)
                        .Build();

                    _stateSubject.OnNext(nextState);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during uEye state update.");
                }
                await Task.Delay(TimeSpan.FromSeconds(2), _pollingCts.Token);
            }
        }, _pollingCts.Token);
    }

    public override async Task<CommandResult> ExecuteCommand(string command, List<DeviceCommandArgument> arguments, CancellationToken token)
    {
        if (!Enum.TryParse<UEyeCameraCommand>(command, out var cmd))
        {
            return new CommandResult { Success = false, Error = $"Unsupported command: {command}" };
        }

        try
        {
            switch (cmd)
            {
                case UEyeCameraCommand.CaptureImage:
                    var bmp = await _backend.CaptureImageAsync(token);
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        var bytes = ms.ToArray();
                        return new CommandResult 
                        { 
                            Success = true, 
                            Result = AresValueHelper.CreateBytes(bytes) 
                        };
                    }

                case UEyeCameraCommand.ApplySettings:
                    await _backend.ApplySettingsAsync(_settings, token);
                    return new CommandResult { Success = true };

                case UEyeCameraCommand.GetStatus:
                    return new CommandResult { Success = true, Result = AresValueHelper.CreateBool(_backend.IsOpened) };

                default:
                    return new CommandResult { Success = false, Error = "Not implemented." };
            }
        }
        catch (Exception ex)
        {
            return new CommandResult { Success = false, Error = ex.Message };
        }
    }

    public override Task UpdateSettings(AresStruct settings)
    {
        _settings = UEyeCameraSettings.FromStruct(settings);
        return _backend.ApplySettingsAsync(_settings, CancellationToken.None);
    }

    public override Task<AresStruct> GetSettings()
    {
        return Task.FromResult(_settings.ToStruct());
    }

    protected override Task<List<DeviceCommandDescriptor>> BuildCommandDescriptorsAsync()
    {
        return Task.FromResult(new List<DeviceCommandDescriptor>
        {
            new DeviceCommandDescriptor
            {
                Name = UEyeCameraCommand.CaptureImage.ToString(),
                Description = "Captures a single frame and returns it as Png bytes.",
                OutputSchema = AresSchemaBuilder.Entry(AresDataType.ByteArray).Build()
            },
            new DeviceCommandDescriptor
            {
                Name = UEyeCameraCommand.ApplySettings.ToString(),
                Description = "Applies current settings to the hardware."
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        _pollingCts.Cancel();
        await _pollingTask;
        _backend.Dispose();
        _stateSubject.OnCompleted();
    }
}
