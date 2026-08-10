using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace AlicatBusControl.Calculations
{


    public sealed class FlowCompositionSolver : IFlowCompositionSolver
    {

      
        private const double CompositionSumTolerance = 1e-8;
        private const double FlowTolerance = 1e-8;
        private const double ResidualTolerance = 1e-8;

        /// <summary>
        /// Builds and validates the static flow-composition model.
        /// This should normally be called once when the bus controller
        /// is initialized.
        /// </summary>
        public FlowCompositionModel BuildModel(
            IReadOnlyList<FlowComponent> flowComponents)
        {
            ArgumentNullException.ThrowIfNull(flowComponents);

            ValidateFlowComponents(flowComponents);

            var gases = flowComponents
                .SelectMany(component => component.Components)
                .Select(component => component.Gas.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    gas => gas,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (gases.Length == 0)
            {
                throw new InvalidOperationException(
                    "No gases were found in the configured flow components.");
            }

            var matrix = BuildCompositionMatrix(
                gases,
                flowComponents);

            /*
             * Rows are gas equations.
             * Columns are MFC/flow-component unknowns.
             *
             * An underdetermined system has more unknown component flows
             * than gas equations.
             */
            if (matrix.RowCount < matrix.ColumnCount)
            {
                throw new InvalidOperationException(
                    $"The configured flow system is underdetermined. " +
                    $"It contains {matrix.ColumnCount} flow components but " +
                    $"only {matrix.RowCount} gas equations.");
            }

            var decomposition = matrix.Svd();

            /*
             * Full column rank is required for a unique component-flow
             * solution in a square or overdetermined system.
             */
            if (decomposition.Rank < matrix.ColumnCount)
            {
                throw new InvalidOperationException(
                    $"The configured flow-component matrix is rank deficient. " +
                    $"Matrix rank is {decomposition.Rank}, but " +
                    $"{matrix.ColumnCount} independent columns are required. " +
                    "One or more flow-component compositions are redundant " +
                    "or linearly dependent.");
            }

            return new FlowCompositionModel(
                gases,
                flowComponents.ToArray(),
                matrix,
                decomposition);
        }

        /// <summary>
        /// Calculates the required MFC setpoints for a requested target
        /// composition and total flow.
        /// </summary>
        public FlowCalculationResult Calculate(
            FlowCompositionModel model,
            IReadOnlyDictionary<string, double> targetComposition,
            double totalFlow)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(targetComposition);

            var validationErrors = ValidateTarget(
                model,
                targetComposition,
                totalFlow);

            if (validationErrors.Count > 0)
            {
                return FlowCalculationResult.Invalid(
                    validationErrors);
            }

            var targetVector = BuildTargetVector(
                model.Gases,
                targetComposition,
                totalFlow);

            Vector<double> solution;

            try
            {
                solution = model.Decomposition.Solve(targetVector);
            }
            catch (Exception exception)
            {
                return FlowCalculationResult.Invalid(
                    $"The flow calculation failed: {exception.Message}");
            }

            if (solution.Count != model.FlowComponents.Count)
            {
                return FlowCalculationResult.Invalid(
                    $"The solver returned {solution.Count} flow values, " +
                    $"but {model.FlowComponents.Count} values were expected.");
            }

            if (solution.Any(flow => !double.IsFinite(flow)))
            {
                return FlowCalculationResult.Invalid(
                    "The flow calculation returned one or more non-finite values.");
            }

            var calculatedTarget =
                model.CompositionMatrix * solution;

            var residualVector =
                calculatedTarget - targetVector;

            var residualNorm =
                residualVector.L2Norm();

            var allowedResidual =
                ResidualTolerance *
                Math.Max(1.0, targetVector.L2Norm());

            if (residualNorm > allowedResidual)
            {
                return BuildUnachievableTargetResult(
                    model,
                    targetVector,
                    calculatedTarget,
                    residualNorm,
                    allowedResidual);
            }

            var setpoints = new List<FlowSetpoint>(
                model.FlowComponents.Count);

            for (var index = 0;
                 index < model.FlowComponents.Count;
                 index++)
            {
                var flowComponent =
                    model.FlowComponents[index];

                var calculatedFlow =
                    solution[index];

                /*
                 * Small negative values may result from floating-point
                 * roundoff and are clamped to zero.
                 */
                if (calculatedFlow < -FlowTolerance)
                {
                    return FlowCalculationResult.Invalid(
                        $"The requested composition requires a negative flow " +
                        $"of {calculatedFlow:G12} for flow component " +
                        $"'{flowComponent.DeviceName}'.");
                }

                calculatedFlow =
                    Math.Max(0.0, calculatedFlow);

                setpoints.Add(
                    new FlowSetpoint(
                        flowComponent.DeviceName,
                        calculatedFlow));
            }

            var calculatedTotalFlow =
                setpoints.Sum(setpoint => setpoint.Flow);

            var allowedTotalFlowError =
                FlowTolerance * Math.Max(1.0, totalFlow);

            var totalFlowError =
                Math.Abs(calculatedTotalFlow - totalFlow);

            if (totalFlowError > allowedTotalFlowError)
            {
                return FlowCalculationResult.Invalid(
                    $"The calculated component flows sum to " +
                    $"{calculatedTotalFlow:G12}, but the requested total " +
                    $"flow is {totalFlow:G12}. The difference is " +
                    $"{totalFlowError:G12}.");
            }

            var achievedComposition =
                BuildAchievedComposition(
                    model,
                    calculatedTarget,
                    calculatedTotalFlow);

            return FlowCalculationResult.Valid(
                setpoints,
                achievedComposition,
                residualNorm);
        }

        private static Matrix<double> BuildCompositionMatrix(
            IReadOnlyList<string> gases,
            IReadOnlyList<FlowComponent> flowComponents)
        {
            var matrix = Matrix<double>.Build.Dense(
                gases.Count,
                flowComponents.Count);

            for (var column = 0;
                 column < flowComponents.Count;
                 column++)
            {
                var flowComponent =
                    flowComponents[column];

                /*
                 * Grouping allows duplicate entries for the same gas to be
                 * combined deterministically, although duplicate entries
                 * are rejected during validation.
                 */
                var componentComposition = flowComponent
                    .Components
                    .GroupBy(
                        component => component.Gas.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(
                            component => component.Concentration),
                        StringComparer.OrdinalIgnoreCase);

                for (var row = 0;
                     row < gases.Count;
                     row++)
                {
                    matrix[row, column] =
                        componentComposition.TryGetValue(
                            gases[row],
                            out var concentration)
                            ? concentration
                            : 0.0;
                }
            }

            return matrix;
        }

        private static Vector<double> BuildTargetVector(
            IReadOnlyList<string> gases,
            IReadOnlyDictionary<string, double> targetComposition,
            double totalFlow)
        {
            /*
             * Copy into a case-insensitive dictionary in case the caller's
             * dictionary uses a case-sensitive comparer.
             */
            var normalizedTarget =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var pair in targetComposition)
            {
                normalizedTarget[pair.Key.Trim()] =
                    pair.Value;
            }

            return Vector<double>.Build.Dense(
                gases.Count,
                index =>
                {
                    var gas = gases[index];

                    return normalizedTarget.TryGetValue(
                        gas,
                        out var concentration)
                        ? concentration * totalFlow
                        : 0.0;
                });
        }

        private static IReadOnlyDictionary<string, double>
            BuildAchievedComposition(
                FlowCompositionModel model,
                Vector<double> calculatedTarget,
                double calculatedTotalFlow)
        {
            var achieved =
                new Dictionary<string, double>(
                    StringComparer.OrdinalIgnoreCase);

            for (var index = 0;
                 index < model.Gases.Count;
                 index++)
            {
                achieved[model.Gases[index]] =
                    calculatedTotalFlow > 0.0
                        ? calculatedTarget[index] /
                          calculatedTotalFlow
                        : 0.0;
            }

            return achieved;
        }

        private static FlowCalculationResult
            BuildUnachievableTargetResult(
                FlowCompositionModel model,
                Vector<double> requestedTarget,
                Vector<double> calculatedTarget,
                double residualNorm,
                double allowedResidual)
        {
            var gasErrors = new List<string>();

            for (var index = 0;
                 index < model.Gases.Count;
                 index++)
            {
                var difference =
                    calculatedTarget[index] -
                    requestedTarget[index];

                if (Math.Abs(difference) <= allowedResidual)
                {
                    continue;
                }

                gasErrors.Add(
                    $"{model.Gases[index]}: requested " +
                    $"{requestedTarget[index]:G12}, achievable " +
                    $"{calculatedTarget[index]:G12}, difference " +
                    $"{difference:G12}");
            }

            var details = gasErrors.Count > 0
                ? $" Gas residuals: {string.Join("; ", gasErrors)}."
                : string.Empty;

            return FlowCalculationResult.Invalid(
                $"The requested gas composition cannot be produced exactly " +
                $"by the configured flow components. Residual norm is " +
                $"{residualNorm:G12}; allowed residual is " +
                $"{allowedResidual:G12}.{details}");
        }

        private static void ValidateFlowComponents(
            IReadOnlyList<FlowComponent> flowComponents)
        {
            if (flowComponents.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one flow component must be configured.");
            }

            var duplicateNames = flowComponents
                .Where(component =>
                    !string.IsNullOrWhiteSpace(component.DeviceName))
                .GroupBy(
                    component => component.DeviceName.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            if (duplicateNames.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Duplicate flow-component names were found: " +
                    $"{string.Join(", ", duplicateNames)}.");
            }

            foreach (var flowComponent in flowComponents)
            {
                if (flowComponent is null)
                {
                    throw new InvalidOperationException(
                        "A configured flow component is null.");
                }

                if (string.IsNullOrWhiteSpace(flowComponent.DeviceName))
                {
                    throw new InvalidOperationException(
                        "Every flow component must have an MFC name.");
                }

                if (flowComponent.Components is null ||
                    flowComponent.Components.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Flow component '{flowComponent.DeviceName}' has no gas " +
                        "composition entries.");
                }

                var duplicateGases = flowComponent
                    .Components
                    .Where(component =>
                        !string.IsNullOrWhiteSpace(component.Gas))
                    .GroupBy(
                        component => component.Gas.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToArray();

                if (duplicateGases.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"Flow component '{flowComponent.DeviceName}' contains " +
                        $"duplicate gas entries: " +
                        $"{string.Join(", ", duplicateGases)}.");
                }

                foreach (var gasComponent in
                         flowComponent.Components)
                {
                    if (gasComponent is null)
                    {
                        throw new InvalidOperationException(
                            $"Flow component '{flowComponent.DeviceName}' contains " +
                            "a null gas component.");
                    }

                    if (string.IsNullOrWhiteSpace(
                            gasComponent.Gas))
                    {
                        throw new InvalidOperationException(
                            $"Flow component '{flowComponent.DeviceName}' contains " +
                            "a gas component with no gas name.");
                    }

                    if (!double.IsFinite(
                            gasComponent.Concentration))
                    {
                        throw new InvalidOperationException(
                            $"Gas '{gasComponent.Gas}' in flow component " +
                            $"'{flowComponent.DeviceName}' has a non-finite " +
                            "concentration.");
                    }

                    if (gasComponent.Concentration < 0.0 ||
                        gasComponent.Concentration > 1.0)
                    {
                        throw new InvalidOperationException(
                            $"Gas '{gasComponent.Gas}' in flow component " +
                            $"'{flowComponent.DeviceName}' has concentration " +
                            $"{gasComponent.Concentration:G12}. " +
                            "Concentrations must be fractions between " +
                            "0 and 1.");
                    }
                }

                var compositionSum =
                    flowComponent.Components.Sum(
                        component => component.Concentration);

                if (Math.Abs(compositionSum - 1.0) >
                    CompositionSumTolerance)
                {
                    throw new InvalidOperationException(
                        $"Gas concentrations for flow component " +
                        $"'{flowComponent.DeviceName}' sum to " +
                        $"{compositionSum:G12}. They must sum to 1.");
                }
            }
        }

        private static List<string> ValidateTarget(
            FlowCompositionModel model,
            IReadOnlyDictionary<string, double> targetComposition,
            double totalFlow)
        {
            var errors = new List<string>();

            if (!double.IsFinite(totalFlow) ||
                totalFlow <= 0.0)
            {
                errors.Add(
                    "Total flow must be a finite value greater than zero.");
            }

            if (targetComposition.Count == 0)
            {
                errors.Add(
                    "No target gas composition has been specified.");

                return errors;
            }

            var availableGases =
                new HashSet<string>(
                    model.Gases,
                    StringComparer.OrdinalIgnoreCase);

            var normalizedNames =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var pair in targetComposition)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    errors.Add(
                        "A target-composition entry has no gas name.");

                    continue;
                }

                var normalizedGas =
                    pair.Key.Trim();

                if (!normalizedNames.Add(normalizedGas))
                {
                    errors.Add(
                        $"The target composition contains duplicate entries " +
                        $"for gas '{normalizedGas}'.");
                }

                if (!availableGases.Contains(normalizedGas))
                {
                    errors.Add(
                        $"Target gas '{normalizedGas}' is not available from " +
                        "any configured flow component.");
                }

                if (!double.IsFinite(pair.Value))
                {
                    errors.Add(
                        $"Target concentration for gas '{normalizedGas}' " +
                        "must be finite.");

                    continue;
                }

                if (pair.Value < 0.0 ||
                    pair.Value > 1.0)
                {
                    errors.Add(
                        $"Target concentration for gas '{normalizedGas}' is " +
                        $"{pair.Value:G12}. Concentrations must be fractions " +
                        "between 0 and 1.");
                }
            }

            var targetSum =
                targetComposition.Values
                    .Where(double.IsFinite)
                    .Sum();

            if (Math.Abs(targetSum - 1.0) >
                CompositionSumTolerance)
            {
                errors.Add(
                    $"Target concentrations sum to {targetSum:G12}. " +
                    "They must sum to 1.");
            }

            return errors;
        }

    }

    /// <summary>
    /// Immutable static model created from the configured flow components.
    /// </summary>





}