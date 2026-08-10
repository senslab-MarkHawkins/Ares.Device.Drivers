using Ares.Datamodel;

namespace AlicatBusControl.Models
{
    public sealed class MassFlowControllerSnapshot
    {
        public MassFlowControllerSnapshot(
            string name,
            double massFlow,
            double setpoint)
        {
            Name = name;
            MassFlow = massFlow;
            Setpoint = setpoint;
        }

        public string Name { get; }

        public double MassFlow { get; }

        public double Setpoint { get; }

        public static MassFlowControllerSnapshot FromAresStruct(
            AresStruct state)
        {
            ArgumentNullException.ThrowIfNull(state);

            var name =
                state.Fields
                    .GetValueOrDefault("Name")
                    ?.StringValue;

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException(
                    "MFC state does not contain a valid Name field.");
            }

            if (!state.Fields.TryGetValue(
                    "LiveData",
                    out var liveDataValue) ||
                liveDataValue.StructValue is null)
            {
                throw new InvalidOperationException(
                    $"MFC '{name}' has not published LiveData.");
            }

            var liveFields =
                liveDataValue.StructValue.Fields;

            if (!liveFields.TryGetValue(
                    "MassFlow",
                    out var massFlowValue) ||
                !massFlowValue.HasNumberValue ||
                !double.IsFinite(massFlowValue.NumberValue))
            {
                throw new InvalidOperationException(
                    $"MFC '{name}' does not contain a valid MassFlow value.");
            }

            if (!liveFields.TryGetValue(
                    "Setpoint",
                    out var setpointValue) ||
                !setpointValue.HasNumberValue ||
                !double.IsFinite(setpointValue.NumberValue))
            {
                throw new InvalidOperationException(
                    $"MFC '{name}' does not contain a valid Setpoint value.");
            }

            return new MassFlowControllerSnapshot(
                name,
                massFlowValue.NumberValue,
                setpointValue.NumberValue);
        }
    }
}