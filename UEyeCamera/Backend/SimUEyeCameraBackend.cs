using System.Drawing;
using System.Reflection;

namespace UEyeCamera.Backend;

public class SimUEyeCameraBackend : IUEyeCameraBackend
{
    private bool _isOpened;
    private readonly string _baselinePath;

    public SimUEyeCameraBackend()
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
        _baselinePath = Path.Combine(assemblyDir, "Simulation", "test_baseline.png");
    }

    public bool IsOpened => _isOpened;

    public async Task InitializeAsync(int cameraId, CancellationToken ct)
    {
        _isOpened = true;
    }

    public async Task ApplySettingsAsync(UEyeCameraSettings settings, CancellationToken ct)
    {
        // No-op for simulation
    }

    public async Task<Bitmap> CaptureImageAsync(CancellationToken ct)
    {
        if (File.Exists(_baselinePath))
        {
            return new Bitmap(_baselinePath);
        }

        // Generate a simple pattern if no image exists
        var bmp = new Bitmap(640, 480);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.DarkBlue);
            g.DrawString("uEye SIMULATED", new Font("Arial", 24), Brushes.White, 50, 200);
            g.DrawString(DateTime.Now.ToString("HH:mm:ss.fff"), new Font("Arial", 16), Brushes.LightBlue, 50, 250);
        }
        return bmp;
    }

    public async Task<double> GetFPSAsync(CancellationToken ct)
    {
        return 30.0;
    }

    public async Task<int> GetCaptureStatusAsync(CancellationToken ct)
    {
        return 1000; // Simulated count
    }

    public void Dispose()
    {
        _isOpened = false;
    }
}
