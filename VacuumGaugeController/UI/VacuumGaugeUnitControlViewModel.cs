using Ares.Datamodel;
using Ares.Toolkit.Device.UI;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive.Linq;

namespace VacuumGaugeController.UI;

public partial class VacuumGaugeUnitControlViewModel : DeviceUnitControlViewModel<VacuumGaugeDevice>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;

    public VacuumGaugeUnitControlViewModel(VacuumGaugeDevice device, ILogger<VacuumGaugeUnitControlViewModel> logger) : base(device)
    {
        ViewType = typeof(VacuumGaugeUnitControl);
        
        _stateSubscription = Device.StateStream
            .Subscribe(UpdateState);
    }

    [Reactive]
    public partial VacuumGaugeState State { get; set; } = new();

    private void UpdateState(AresStruct aresState)
    {
        var newState = new VacuumGaugeState
        {
            Pressure = (float)(aresState.Fields.GetValueOrDefault("Pressure")?.NumberValue ?? 0),
            Status = aresState.Fields.GetValueOrDefault("Status")?.StringValue ?? "Unknown",
            ErrorStatus = aresState.Fields.GetValueOrDefault("ErrorStatus")?.StringValue ?? "None"
        };
        State = newState;
    }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
