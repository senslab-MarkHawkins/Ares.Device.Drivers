using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Datamodel.Factories;
using Ares.Toolkit.Device.UI;
using Microsoft.Extensions.Logging;
using WaterPIDRemastered.UI.State;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace WaterPIDRemastered.UI;

public partial class WaterPIDControlViewModel : DeviceUnitControlViewModel<WaterPIDDevice>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;
    private readonly ILogger<WaterPIDControlViewModel> _logger;

    public WaterPIDControlViewModel(WaterPIDDevice device, ILogger<WaterPIDControlViewModel> logger) : base(device)
    {
        _logger = logger;
        DeviceState = new WaterPIDState();
        _stateSubscription = device.StateStream
            .Select(WaterPIDStateMapper.FromAresStruct)
            .Subscribe(UpdateState);

        SetTargetCommand = ReactiveCommand.CreateFromTask<double, CommandResult>(target => 
            device.ExecuteCommand("SetTargetPPM", new List<DeviceCommandArgument> 
            { 
                new() { ArgName = "Target", ArgValue = AresValueHelper.CreateNumber(target) } 
            }, CancellationToken.None));

        ResetCommand = ReactiveCommand.CreateFromTask<CommandResult>(() => 
            device.ExecuteCommand("Reset", new(), CancellationToken.None));

        ViewType = typeof(WaterPIDControl);
        DefaultWidth = 20;
    }

    [Reactive]
    public partial WaterPIDState DeviceState { get; set; }

    [Reactive]
    public partial bool HasValidData { get; private set; }

    [Reactive]
    public partial double NewTarget { get; set; }

    public ReactiveCommand<double, CommandResult> SetTargetCommand { get; }
    public ReactiveCommand<Unit, CommandResult> ResetCommand { get; }

    private void UpdateState(WaterPIDState state)
    {
        DeviceState = state;
        if (NewTarget == 0) NewTarget = state.TargetPPM;
        HasValidData = true;
    }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
    }
}
