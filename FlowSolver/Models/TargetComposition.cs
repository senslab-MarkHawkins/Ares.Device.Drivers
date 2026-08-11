namespace FlowSolver.Models;

public sealed class TargetComposition
{
    private readonly Dictionary<string, CompositionComponent> _components =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<CompositionComponent> Components =>
        _components.Values;

    public void Set(
        string component,
        double? value,
        CompositionUnit unit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);

        var normalizedName = component.Trim();

        _components[normalizedName] =
            new CompositionComponent(
                normalizedName,
                unit,
                value);
    }

    public void Clear()
    {
        _components.Clear();
    }
}