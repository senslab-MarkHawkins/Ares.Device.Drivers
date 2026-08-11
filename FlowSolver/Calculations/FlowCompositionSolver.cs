using FlowSolver.Composition;
using FlowSolver.Models;
using MathNet.Numerics.LinearAlgebra;

namespace FlowSolver.Calculations;

public sealed class FlowCompositionSolver
{
    private const double CompositionTolerance = 1e-8;
    private const double FlowTolerance = 1e-8;
    private const double ResidualTolerance = 1e-8;

    private readonly CompositionBuilder _compositionBuilder;

    public FlowCompositionSolver(
        CompositionBuilder compositionBuilder)
    {
        ArgumentNullException.ThrowIfNull(compositionBuilder);

        _compositionBuilder = compositionBuilder;
    }

    public FlowCompositionModel BuildModel(
        IReadOnlyList<FlowComponent> flowComponents)
    {
        ArgumentNullException.ThrowIfNull(flowComponents);

        ValidateFlowComponents(flowComponents);

        /*
         * Resolve every configured source composition once.
         *
         * FlowComponent.Components may contain:
         *
         *   CO  = 20 Ppm
         *   Air = Balance
         *
         * CompositionBuilder converts that into terminal gases:
         *
         *   CO = ...
         *   N2 = ...
         *   O2 = ...
         */
        var resolvedComponents =
            flowComponents
                .Select(flowComponent =>
                    new ResolvedFlowComponent(
                        flowComponent,
                        _compositionBuilder.Build(
                            flowComponent.Components)))
                .ToArray();

        /*
         * Matrix rows consist only of terminal gases produced after
         * resolving Balance, units, and known mixtures.
         */
        var gases =
            resolvedComponents
                .SelectMany(component =>
                    component.Composition.Keys)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    gas => gas,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (gases.Length == 0)
        {
            throw new InvalidOperationException(
                "No gases were produced by the configured flow components.");
        }

        var matrix =
            BuildCompositionMatrix(
                gases,
                resolvedComponents);

        /*
         * More flow components than independent gas equations means
         * the system is underdetermined.
         */
        if (matrix.RowCount < matrix.ColumnCount)
        {
            throw new InvalidOperationException(
                $"Flow configuration is underdetermined. " +
                $"{matrix.ColumnCount} flow components are configured " +
                $"but only {matrix.RowCount} gas equations exist.");
        }

        var decomposition =
            matrix.Svd();

        /*
         * Full column rank is required for a unique solution.
         */
        if (decomposition.Rank <
            matrix.ColumnCount)
        {
            throw new InvalidOperationException(
                $"Flow configuration is rank deficient. " +
                $"Matrix rank is {decomposition.Rank}; " +
                $"{matrix.ColumnCount} independent columns are required.");
        }

        return new FlowCompositionModel(
            gases,
            flowComponents.ToArray(),
            matrix,
            decomposition);
    }

    public FlowCalculationResult Calculate(
        FlowCompositionModel model,
        IReadOnlyDictionary<string, double> targetComposition,
        double totalFlow)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(targetComposition);

        var errors =
            ValidateTarget(
                model,
                targetComposition,
                totalFlow);

        if (errors.Count > 0)
        {
            return FlowCalculationResult.Invalid(
                errors.ToArray());
        }

        var targetVector =
            BuildTargetVector(
                model.Gases,
                targetComposition,
                totalFlow);

        Vector<double> solution;

        try
        {
            solution =
                model.Decomposition
                    .Solve(targetVector);
        }
        catch (Exception exception)
        {
            return FlowCalculationResult.Invalid(
                $"Unable to solve flow system: " +
                $"{exception.Message}");
        }

        if (solution.Any(
                value => !double.IsFinite(value)))
        {
            return FlowCalculationResult.Invalid(
                "Solver returned one or more non-finite flow values.");
        }

        var calculatedTarget =
            model.CompositionMatrix *
            solution;

        var residualNorm =
            (calculatedTarget - targetVector)
                .L2Norm();

        var allowedResidual =
            ResidualTolerance *
            Math.Max(
                1.0,
                targetVector.L2Norm());

        if (residualNorm >
            allowedResidual)
        {
            return FlowCalculationResult.Invalid(
                $"Requested composition cannot be produced. " +
                $"Residual norm is {residualNorm:G12}; " +
                $"allowed residual is {allowedResidual:G12}.");
        }

        var setpoints =
            new List<FlowSetpoint>(
                model.FlowComponents.Count);

        for (var index = 0;
             index < solution.Count;
             index++)
        {
            var flow =
                solution[index];

            /*
             * Significant negative flows mean the requested composition
             * cannot be physically produced with these sources.
             */
            if (flow < -FlowTolerance)
            {
                return FlowCalculationResult.Invalid(
                    $"Requested composition requires negative flow " +
                    $"{flow:G12} for " +
                    $"'{model.FlowComponents[index].DeviceName}'.");
            }

            /*
             * Clamp insignificant floating-point negative values.
             */
            flow =
                Math.Max(0.0, flow);

            setpoints.Add(
                new FlowSetpoint(
                    model.FlowComponents[index].DeviceName,
                    flow));
        }

        var calculatedTotalFlow =
            setpoints.Sum(
                setpoint => setpoint.Flow);

        if (Math.Abs(
                calculatedTotalFlow -
                totalFlow) >
            FlowTolerance *
            Math.Max(1.0, totalFlow))
        {
            return FlowCalculationResult.Invalid(
                $"Calculated component flows total " +
                $"{calculatedTotalFlow:G12}, but requested total flow " +
                $"is {totalFlow:G12}.");
        }

        var achievedComposition =
            new Dictionary<string, double>(
                StringComparer.OrdinalIgnoreCase);

        for (var index = 0;
             index < model.Gases.Count;
             index++)
        {
            achievedComposition[
                model.Gases[index]] =
                calculatedTotalFlow > 0.0
                    ? calculatedTarget[index] /
                      calculatedTotalFlow
                    : 0.0;
        }

        return FlowCalculationResult.Valid(
            setpoints,
            achievedComposition,
            residualNorm);
    }

    private static Matrix<double>
        BuildCompositionMatrix(
            IReadOnlyList<string> gases,
            IReadOnlyList<ResolvedFlowComponent> flowComponents)
    {
        var matrix =
            Matrix<double>.Build.Dense(
                gases.Count,
                flowComponents.Count);

        for (var column = 0;
             column < flowComponents.Count;
             column++)
        {
            var composition =
                flowComponents[column].Composition;

            for (var row = 0;
                 row < gases.Count;
                 row++)
            {
                matrix[row, column] =
                    composition.TryGetValue(
                        gases[row],
                        out var concentration)
                        ? concentration
                        : 0.0;
            }
        }

        return matrix;
    }

    private static Vector<double>
        BuildTargetVector(
            IReadOnlyList<string> gases,
            IReadOnlyDictionary<string, double> targetComposition,
            double totalFlow)
    {
        return Vector<double>.Build.Dense(
            gases.Count,
            index =>
                targetComposition.TryGetValue(
                    gases[index],
                    out var concentration)
                    ? concentration *
                      totalFlow
                    : 0.0);
    }

    private static List<string> ValidateTarget(
        FlowCompositionModel model,
        IReadOnlyDictionary<string, double> targetComposition,
        double totalFlow)
    {
        var errors =
            new List<string>();

        if (!double.IsFinite(totalFlow) ||
            totalFlow < 0.0)
        {
            errors.Add(
                "Total flow must be finite and non-negative.");
        }

        var availableGases =
            new HashSet<string>(
                model.Gases,
                StringComparer.OrdinalIgnoreCase);

        foreach (var pair in targetComposition)
        {
            if (!availableGases.Contains(pair.Key))
            {
                errors.Add(
                    $"Gas '{pair.Key}' is not available.");
            }

            if (!double.IsFinite(pair.Value) ||
                pair.Value < 0.0 ||
                pair.Value > 1.0)
            {
                errors.Add(
                    $"Concentration for '{pair.Key}' must be " +
                    "between 0 and 1.");
            }
        }

        var sum =
            targetComposition.Values.Sum();

        /*
         * A zero-flow target may legitimately contain an empty or
         * zero-valued composition.
         */
        if (totalFlow > 0.0 &&
            Math.Abs(sum - 1.0) >
            CompositionTolerance)
        {
            errors.Add(
                $"Target composition sums to {sum:G12}; " +
                "it must sum to 1.");
        }

        return errors;
    }

    private static void ValidateFlowComponents(
        IReadOnlyList<FlowComponent> flowComponents)
    {
        if (flowComponents.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one flow component is required.");
        }

        var duplicateDevices =
            flowComponents
                .Where(component =>
                    !string.IsNullOrWhiteSpace(
                        component.DeviceName))
                .GroupBy(
                    component =>
                        component.DeviceName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Where(group =>
                    group.Count() > 1)
                .Select(group =>
                    group.Key)
                .ToArray();

        if (duplicateDevices.Length > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate flow controllers: " +
                $"{string.Join(", ", duplicateDevices)}.");
        }

        foreach (var flowComponent in
                 flowComponents)
        {
            if (string.IsNullOrWhiteSpace(
                    flowComponent.DeviceName))
            {
                throw new InvalidOperationException(
                    "Every flow component requires a device name.");
            }

            if (flowComponent.Components.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Flow component '{flowComponent.DeviceName}' " +
                    "has no composition components.");
            }

            /*
             * Do not validate units, Balance, or component sums here.
             * CompositionBuilder owns those rules and is called by
             * BuildModel().
             */
        }
    }

    private sealed record ResolvedFlowComponent(
        FlowComponent FlowComponent,
        IReadOnlyDictionary<string, double> Composition);
}