using Ares.Toolkit.Device.UI;
using Lambda103Shutter.UI.State;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;

namespace Lambda103Shutter.UI;

public partial class Lambda103UnitControlViewModel : DeviceUnitControlViewModel<Lambda103ShutterDevice>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;

    public Lambda103UnitControlViewModel(Lambda103ShutterDevice device) : base(device)
    {
        ViewType = typeof(Lambda103UnitControl);
        DefaultWidth = 10;

        _stateSubscription = device.StateStream
            .Select(Lambda103StateMapper.FromAresStruct)
            .Subscribe(s => State = s);

        ToggleShutterCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            await Device.SetShutter(!State.ShutterOpen);
        });

        SetWheelCommand = ReactiveCommand.CreateFromTask<int>(async pos =>
        {
            await Device.SetWheel(pos);
        });
    }

    [Reactive]
    public partial Lambda103State State { get; set; } = new();

    public ReactiveCommand<Unit, Unit> ToggleShutterCommand { get; }
    public ReactiveCommand<int, Unit> SetWheelCommand { get; }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
    }
}
