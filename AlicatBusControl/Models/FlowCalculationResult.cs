namespace AlicatBusControl.Calculations
{
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
            ArgumentNullException.ThrowIfNull(setpoints);
            ArgumentNullException.ThrowIfNull(achievedComposition);

            return new FlowCalculationResult(
                success: true,
                setpoints: setpoints,
                achievedComposition: achievedComposition,
                errors: Array.Empty<string>(),
                residualNorm: residualNorm);
        }

        public static FlowCalculationResult Invalid(
            params string[] errors)
        {
            return Invalid((IReadOnlyList<string>)errors);
        }

        public static FlowCalculationResult Invalid(
            IReadOnlyList<string> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            return new FlowCalculationResult(
                success: false,
                setpoints: Array.Empty<FlowSetpoint>(),
                achievedComposition:
                    new Dictionary<string, double>(
                        StringComparer.OrdinalIgnoreCase),
                errors: errors,
                residualNorm: double.NaN);
        }
    }
}
