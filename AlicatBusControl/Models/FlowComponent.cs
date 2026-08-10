using Ares.Datamodel;

namespace AlicatBusControl.Calculations
{
    public sealed class FlowComponent
    {
        public static IReadOnlyList<FlowComponent> ComponentsFromAres(AresStruct deviceSettings)
        {
            return deviceSettings.Fields["FlowComponents"].ListValue.Values.Select(fc => FromAres(fc.StructValue)).ToList();
        }
        public static FlowComponent FromAres(AresStruct flowComponentStruct)
        {
            var name = flowComponentStruct.Fields["MFC"].StringValue;
            var gasComponents = flowComponentStruct.Fields["GasComponents"].ListValue;
            var gasComponentsList = gasComponents.Values.Select(gc => GasComponent.FromAresStruct(gc.StructValue)).ToList();
            return new FlowComponent(name, gasComponentsList);
        }
        public FlowComponent(
            string deviceName,
            IReadOnlyList<GasComponent> components,
            double minimumFlow = 0.0,
            double maximumFlow = double.PositiveInfinity)
        {
            DeviceName = deviceName;
            Components = components;
            MinimumFlow = minimumFlow;
            MaximumFlow = maximumFlow;
        }

        public string DeviceName { get; }

        public IReadOnlyList<GasComponent> Components { get; }

        public double MinimumFlow { get; }

        public double MaximumFlow { get; }

    }
}
