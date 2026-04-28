using System.Drawing;

namespace UEyeCamera.Backend;

public interface IUEyeCameraBackend : IDisposable
{
    Task InitializeAsync(int cameraId, CancellationToken ct);
    Task ApplySettingsAsync(UEyeCameraSettings settings, CancellationToken ct);
    Task<Bitmap> CaptureImageAsync(CancellationToken ct);
    Task<double> GetFPSAsync(CancellationToken ct);
    Task<int> GetCaptureStatusAsync(CancellationToken ct);
    bool IsOpened { get; }
}
