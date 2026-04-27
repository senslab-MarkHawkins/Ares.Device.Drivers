using Ares.Datamodel;
using Ares.Datamodel.Device;
using Ares.Datamodel.Extensions;
using Ares.Toolkit.Device.UI;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Reactive;
using System.Reactive.Linq;
using NiWaterValve.UI.State;
using NiWaterValve.Enums;

namespace NiWaterValve.UI;

public partial class NiWaterValveUnitControlViewModel : DeviceUnitControlViewModel<NiWaterValveDevice>, IAsyncDisposable
{
    private IDisposable? _stateSubscription;

    public NiWaterValveUnitControlViewModel(NiWaterValveDevice device, ILogger<NiWaterValveUnitControlViewModel> logger) : base(device)
    {
        ViewType = typeof(NiWaterValveUnitControl);
        DefaultWidth = 12;

        _stateSubscription = Device.StateStream
            .Select(NiWaterValveStateMapper.FromAresStruct)
            .Subscribe(s => State = s);

        SetVoltageCommand = ReactiveCommand.CreateFromTask<double, CommandResult>(async v => 
            await Device.ExecuteCommand(NiWaterValveCommand.SetValveVoltage.ToString(), 
            [new DeviceCommandArgument { ArgName = "Voltage", ArgValue = AresValueHelper.CreateNumber(v) }], 
            CancellationToken.None));
    }

    [Reactive]
    public partial NiWaterValveState State { get; set; } = new();

    [Reactive]
    public partial double TargetVoltage { get; set; }

    public ReactiveCommand<double, CommandResult> SetVoltageCommand { get; }

    public async ValueTask DisposeAsync()
    {
        _stateSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}
