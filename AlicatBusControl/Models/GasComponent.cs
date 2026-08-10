using Ares.Datamodel;

namespace AlicatBusControl.Calculations
{
    public sealed class GasComponent
    {
        public static GasComponent FromAresStruct(AresStruct aresStruct)
        {
            var gas = aresStruct.Fields["Gas"].StringValue;
            var concentration = aresStruct.Fields["Concentration"].NumberValue;

            if (string.IsNullOrWhiteSpace(gas))
            {
                throw new InvalidOperationException(
                    "A gas component must include a gas name.");
            }

            if (!double.IsFinite(concentration) ||
                concentration < 0.0 ||
                concentration > 1.0)
            {
                throw new InvalidOperationException(
                    $"Gas '{gas}' has an invalid concentration of {concentration}. " +
                    "Concentrations must be fractions from 0 to 1.");
            }

            return new GasComponent(gas, concentration);
        }

        public GasComponent(string gas, double concentration)
        {
            Gas = gas;
            Concentration = concentration;
        }

        public string Gas { get; }

        public double Concentration { get; }
    }
}
