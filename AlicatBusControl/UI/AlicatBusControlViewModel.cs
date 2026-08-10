using AlicatBusControl.UI.State;
using Ares.Toolkit.Device.UI;
using System.Globalization;
using System.Reactive.Linq;

namespace AlicatBusControl.UI;

public sealed class AlicatBusControlViewModel :
    DeviceUnitControlViewModel<AlicatBusController>,
    IAsyncDisposable
{
    private readonly ILogger<AlicatBusControlViewModel> _logger;
    private readonly IDisposable _stateSubscription;

    //private readonly Dictionary<string, string> _inputValues;

    public AlicatBusControlViewModel(
        AlicatBusController controller,
        ILogger<AlicatBusControlViewModel> logger)
        : base(controller)
    {
        _logger = logger;

        _logger.LogInformation(
            "Alicat bus controller view model initialized.");

        ViewType = typeof(AlicatBusControl);
        DefaultWidth = 19;

        State = AlicatBusStateMapper.EmptyFromSchema(
            controller.StateSchema);

        _stateSubscription = controller.StateStream
            .Select(state =>
                AlicatBusStateMapper.FromAresStruct(
                    state,
                    controller.StateSchema))
            .Subscribe(
                UpdateState,
                exception =>
                    _logger.LogError(
                        exception,
                        "Alicat bus state subscription failed."));
    }

    public AlicatBusState State { get; private set; }

    public event Action? StateChanged;

    //public string GetInputValue(string propertyName)
    //{
    //    return _inputValues.TryGetValue(
    //               propertyName,
    //               out var value)
    //        ? value
    //        : string.Empty;
    //}

    //public void SetInputValue(
    //    string propertyName,
    //    string? value)
    //{
    //    _inputValues[propertyName] =
    //        value ?? string.Empty;
    //}

    public async Task<bool> UpdateSettingsAsync()
    {
        foreach (var property in State.Properties)
        {
            if (!property.InputValue.HasValue)
            {
                continue;
            }

            var value =
                property.InputValue.Value;

            if (string.Equals(
                    property.Name,
                    "TotalFlow",
                    StringComparison.OrdinalIgnoreCase))
            {
                await Device.SetTargetFlow(value);
            }
            else
            {
                await Device.SetTargetComposition(
                    property.Name,
                    value);
            }
        }

        return true;
    }

    public Task<bool> ApplyFlowAsync()
    {
        _logger.LogInformation($"Attempting to apply flow.");
        return Device.ApplyFlow();
    }

    private void UpdateState(
        AlicatBusState newState)
    {
        foreach (var property in newState.Properties)
        {
            var existing =
                State.Properties.FirstOrDefault(
                    p => string.Equals(
                        p.Name,
                        property.Name,
                        StringComparison.OrdinalIgnoreCase));

            property.InputValue =
                existing?.InputValue ??
                property.TargetValue;
        }

        State = newState;

        StateChanged?.Invoke();
    }

    //private void InitializeInputValues(
    //    AlicatBusState state)
    //{
    //    foreach (var property in state.Properties)
    //    {
    //        _inputValues[property.Name] =
    //            FormatValue(property.TargetValue);
    //    }
    //}

    //private static string FormatValue(double value)
    //{
    //    return value.ToString(
    //        "G12",
    //        CultureInfo.InvariantCulture);
    //}

    public ValueTask DisposeAsync()
    {
        _stateSubscription.Dispose();
        return ValueTask.CompletedTask;
    }
}