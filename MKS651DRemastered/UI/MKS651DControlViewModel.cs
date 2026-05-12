using Ares.Datamodel;
using Ares.Toolkit.Device.UI;
using Microsoft.Extensions.Logging;
using MKS651DRemastered.UI.State;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;

namespace MKS651DRemastered.UI;

public partial class MKS651DControlViewModel : DeviceUnitControlViewModel<MKS651D>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;
    private readonly ILogger<MKS651DControlViewModel> _logger;

    public MKS651DControlViewModel(MKS651D device, ILogger<MKS651DControlViewModel> logger) : base(device)
    {
        _logger = logger;
        DeviceState = new MKS651DState();
        _stateSubscription = device.StateStream
            .Select(MKS651DStateMapper.FromAresStruct)
            .Subscribe(UpdateState);

        OpenValveCommand = ReactiveCommand.CreateFromTask(() => device.ExecuteCommand("OpenValve", new(), CancellationToken.None));
        CloseValveCommand = ReactiveCommand.CreateFromTask(() => device.ExecuteCommand("CloseValve", new(), CancellationToken.None));
        
        ViewType = typeof(MKS651DControl);
        DefaultWidth = 20;
    }

    [Reactive]
    public partial MKS651DState DeviceState { get; set; }

    [Reactive]
    public partial bool HasValidData { get; private set; }

    public ReactiveCommand<Unit, CommandResult> OpenValveCommand { get; }
    public ReactiveCommand<Unit, CommandResult> CloseValveCommand { get; }

    private void UpdateState(MKS651DState state)
    {
        DeviceState = state;
        HasValidData = true;
    }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
    }
}
