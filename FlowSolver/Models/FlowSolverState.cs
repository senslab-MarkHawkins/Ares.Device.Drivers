namespace FlowSolver.Models;

public sealed class FlowSolverState
{
    public string Name { get; init; } =
        string.Empty;

    public bool CalculationValid { get; init; }

    public string CalculationError { get; init; } =
        string.Empty;

    public double TargetFlow { get; init; }

    public IReadOnlyDictionary<string, double>
        TargetComposition
    { get; init; } =
            new Dictionary<string, double>();

    public IReadOnlyDictionary<string, double>
        Setpoints
    { get; init; } =
            new Dictionary<string, double>();

    public double ResidualNorm { get; init; }
}