using AlicatBusControl.Models;

namespace AlicatBusControl.Calculations
{
    public interface IFlowCompositionSolver
    {
        FlowCompositionModel BuildModel(
            IReadOnlyList<FlowComponent> flowComponents);

        FlowCalculationResult Calculate(
            FlowCompositionModel model,
            IReadOnlyDictionary<string, double> targetComposition,
            double totalFlow);
    }
}
