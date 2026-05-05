using Ares.Toolkit.Device.UI;
using Microsoft.Extensions.Logging;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;
using ShawHygrometer.UI.State;

namespace ShawHygrometer.UI;

public partial class ShawSuperdewUnitControlViewModel : DeviceUnitControlViewModel<ShawSuperdewHygrometer>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;

    public ShawSuperdewUnitControlViewModel(ShawSuperdewHygrometer device, ILogger<ShawSuperdewUnitControlViewModel> logger) : base(device)
    {
        ViewType = typeof(ShawSuperdewUnitControl);
        DefaultWidth = 12;
        
        _stateSubscription = Device.StateStream
            .Select(HygrometerStateMapper.FromShawAresStruct)
            .Subscribe(s => State = s);
    }

    [Reactive]
    public partial ShawSuperdewState State { get; set; } = new();

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
