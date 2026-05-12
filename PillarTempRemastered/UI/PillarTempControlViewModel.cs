using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Toolkit.Device.UI;
using Microsoft.Extensions.Logging;
using PillarTempRemastered.UI.State;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace PillarTempRemastered.UI;

public partial class PillarTempControlViewModel : DeviceUnitControlViewModel<PillarTempDevice>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;
    private readonly ILogger<PillarTempControlViewModel> _logger;

    public PillarTempControlViewModel(PillarTempDevice device, ILogger<PillarTempControlViewModel> logger) : base(device)
    {
        _logger = logger;
        DeviceState = new PillarTempState();
        _stateSubscription = device.StateStream
            .Select(PillarTempStateMapper.FromAresStruct)
            .Subscribe(UpdateState);

        SetTargetCommand = ReactiveCommand.CreateFromTask<double, CommandResult>(target => 
            device.ExecuteCommand("SetTargetTemperature", new List<DeviceCommandArgument> 
            { 
                new() { ArgName = "Target", ArgValue = AresValueHelper.CreateNumber(target) } 
            }, CancellationToken.None));

        ResetCommand = ReactiveCommand.CreateFromTask<CommandResult>(() => 
            device.ExecuteCommand("Reset", new(), CancellationToken.None));

        ViewType = typeof(PillarTempControl);
        DefaultWidth = 20;
    }

    [Reactive]
    public partial PillarTempState DeviceState { get; set; }

    [Reactive]
    public partial bool HasValidData { get; private set; }

    [Reactive]
    public partial double NewTarget { get; set; }

    public ReactiveCommand<double, CommandResult> SetTargetCommand { get; }
    public ReactiveCommand<Unit, CommandResult> ResetCommand { get; }

    private void UpdateState(PillarTempState state)
    {
        DeviceState = state;
        if (NewTarget == 0) NewTarget = state.TargetTemperature;
        HasValidData = true;
    }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
    }
}
