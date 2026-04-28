using System.Drawing;
using uEye;
using uEye.Defines;
using uEye.Types;

namespace UEyeCamera.Backend;

public class UEyeCameraHardwareBackend : IUEyeCameraBackend
{
    private Camera? _camera;
    private bool _isOpened;

    public bool IsOpened => _isOpened;

    public async Task InitializeAsync(int cameraId, CancellationToken ct)
    {
        _camera = new Camera();
        var status = _camera.Init(cameraId | (int)DeviceEnumeration.UseDeviceID);
        if (status != Status.SUCCESS)
        {
            throw new Exception($"Failed to initialize uEye camera {cameraId}. Status: {status}");
        }

        // Allocate memory (legacy used 3 buffers)
        for (int i = 0; i < 3; i++)
        {
            status = _camera.Memory.Allocate();
            if (status != Status.SUCCESS) break;
        }

        int[] idList;
        _camera.Memory.GetList(out idList);
        _camera.Memory.Sequence.Add(idList);

        _isOpened = true;
    }

    public async Task ApplySettingsAsync(UEyeCameraSettings settings, CancellationToken ct)
    {
        if (_camera == null || !_isOpened) return;

        _camera.Timing.PixelClock.Set(settings.PixelClock);
        _camera.Timing.Framerate.Set(30.0); 
        _camera.Timing.Exposure.Set(settings.Exposure);
        
        _camera.Gain.Hardware.Scaled.SetMaster(settings.Gain);
        _camera.AutoFeatures.Software.WhiteBalance.SetEnable(settings.AutoWhiteBalance);
        _camera.AutoFeatures.Software.Gain.SetEnable(settings.AutoGain);
    }

    public async Task<Bitmap> CaptureImageAsync(CancellationToken ct)
    {
        if (_camera == null || !_isOpened) throw new InvalidOperationException("Camera not opened.");

        // Try Freeze with no arguments
        var status = _camera.Acquisition.Freeze();
        if (status != Status.SUCCESS)
        {
            throw new Exception($"Failed to freeze frame. Status: {status}");
        }

        int memId;
        _camera.Memory.GetActive(out memId);
        _camera.Memory.ToBitmap(memId, out var bitmap);
        
        return (Bitmap)bitmap.Clone();
    }

    public async Task<double> GetFPSAsync(CancellationToken ct)
    {
        if (_camera == null || !_isOpened) return 0;
        double fps;
        _camera.Timing.Framerate.GetCurrentFps(out fps);
        return fps;
    }

    public async Task<int> GetCaptureStatusAsync(CancellationToken ct)
    {
        if (_camera == null || !_isOpened) return 0;
        CaptureStatus status;
        _camera.Information.GetCaptureStatus(out status);
        return (int)status.Total;
    }

    public void Dispose()
    {
        if (_camera != null && _isOpened)
        {
            _camera.Exit();
        }
        _isOpened = false;
    }
}
