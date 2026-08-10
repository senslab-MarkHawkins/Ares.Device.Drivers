namespace AlicatBusControl.UI.State;

public sealed class AlicatBusState
{
    public string Name { get; set; } = string.Empty;

    public string FlowVerificationState { get; set; } = "Unknown";

    public bool ChangesPending { get; set; }

    public IReadOnlyList<AlicatBusPropertyState> Properties { get; set; } =
        Array.Empty<AlicatBusPropertyState>();
}

public sealed class AlicatBusPropertyState
{
    public string Name { get; init; } = string.Empty;

    public double AppliedValue { get; init; }

    public double TargetValue { get; init; }

    public double? InputValue { get; set; }
}