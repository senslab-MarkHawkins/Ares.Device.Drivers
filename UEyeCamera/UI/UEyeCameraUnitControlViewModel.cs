using Ares.Toolkit.Device.UI;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using UEyeCamera.UI.State;

namespace UEyeCamera.UI;

public partial class UEyeCameraUnitControlViewModel : DeviceUnitControlViewModel<UEyeCameraDevice>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;

    public UEyeCameraUnitControlViewModel(UEyeCameraDevice device) : base(device)
    {
        ViewType = typeof(UEyeCameraUnitControl);
        DefaultWidth = 12;

        _stateSubscription = device.StateStream
            .Select(UEyeCameraStateMapper.FromAresStruct)
            .Subscribe(s => State = s);

        CaptureImageCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await Device.ExecuteCommand(UEyeCameraCommand.CaptureImage.ToString(), new(), CancellationToken.None);
            if (result.Success && result.Result != null)
            {
                LatestImageBytes = result.Result.BytesValue.ToByteArray();
            }
        });
    }

    [Reactive]
    public partial UEyeCameraState State { get; set; } = new();

    [Reactive]
    public partial byte[]? LatestImageBytes { get; set; }

    public ReactiveCommand<Unit, Unit> CaptureImageCommand { get; }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
    }
}
