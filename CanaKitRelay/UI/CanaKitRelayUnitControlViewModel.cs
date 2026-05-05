using Ares.Datamodel;
using Ares.Toolkit.Device.UI;
using CanaKitRelay.Enums;
using CanaKitRelay.UI.State;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;

namespace CanaKitRelay.UI;

public partial class CanaKitRelayUnitControlViewModel : DeviceUnitControlViewModel<CanaKitRelayDevice>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;

    public CanaKitRelayUnitControlViewModel(CanaKitRelayDevice device, ILogger<CanaKitRelayUnitControlViewModel> logger) : base(device)
    {
        ViewType = typeof(CanaKitRelayUnitControl);
        DefaultWidth = 12;

        _stateSubscription = Device.StateStream
            .Select(RelayStateMapper.FromAresStruct)
            .Subscribe(s => State = s);

        SetBypassCommand = ReactiveCommand.CreateFromTask(() => 
            Device.ExecuteCommand(CanaKitRelayCommand.SetBypass.ToString(), [], CancellationToken.None));
            
        SetFullVacuumCommand = ReactiveCommand.CreateFromTask(() => 
            Device.ExecuteCommand(CanaKitRelayCommand.SetFullVacuum.ToString(), [], CancellationToken.None));

        ToggleRelay1Command = ReactiveCommand.CreateFromTask(() => 
            Device.ExecuteCommand(CanaKitRelayCommand.ToggleRelay1.ToString(), [], CancellationToken.None));

        ToggleRelay2Command = ReactiveCommand.CreateFromTask(() => 
            Device.ExecuteCommand(CanaKitRelayCommand.ToggleRelay2.ToString(), [], CancellationToken.None));
    }

    [Reactive]
    public partial CanaKitRelayState State { get; set; } = new();

    public ReactiveCommand<Unit, CommandResult> SetBypassCommand { get; }
    public ReactiveCommand<Unit, CommandResult> SetFullVacuumCommand { get; }
    public ReactiveCommand<Unit, CommandResult> ToggleRelay1Command { get; }
    public ReactiveCommand<Unit, CommandResult> ToggleRelay2Command { get; }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
