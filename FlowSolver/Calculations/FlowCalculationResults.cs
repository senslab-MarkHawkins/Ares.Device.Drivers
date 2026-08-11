using FlowSolver.Models;

namespace FlowSolver.Calculations;

public sealed class FlowCalculationResult
{
    private FlowCalculationResult(
        bool success,
        IReadOnlyList<FlowSetpoint> setpoints,
        IReadOnlyDictionary<string, double> achievedComposition,
        IReadOnlyList<string> errors,
        double residualNorm)
    {
        Success = success;
        Setpoints = setpoints;
        AchievedComposition = achievedComposition;
        Errors = errors;
        ResidualNorm = residualNorm;
    }

    public bool Success { get; }

    public IReadOnlyList<FlowSetpoint> Setpoints { get; }

    public IReadOnlyDictionary<string, double>
        AchievedComposition
    { get; }

    public IReadOnlyList<string> Errors { get; }

    public double ResidualNorm { get; }

    public static FlowCalculationResult Valid(
        IReadOnlyList<FlowSetpoint> setpoints,
        IReadOnlyDictionary<string, double> achievedComposition,
        double residualNorm)
    {
        return new FlowCalculationResult(
            true,
            setpoints,
            achievedComposition,
            Array.Empty<string>(),
            residualNorm);
    }

    public static FlowCalculationResult Invalid(
        params string[] errors)
    {
        return new FlowCalculationResult(
            false,
            Array.Empty<FlowSetpoint>(),
            new Dictionary<string, double>(
                StringComparer.OrdinalIgnoreCase),
            errors,
            double.NaN);
    }
}