using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;

namespace AlicatBusControl.Calculations
{
    public sealed class FlowCompositionModel
    {
        public FlowCompositionModel(
            IReadOnlyList<string> gases,
            IReadOnlyList<FlowComponent> flowComponents,
            Matrix<double> compositionMatrix,
            Svd<double> decomposition)
        {
            ArgumentNullException.ThrowIfNull(gases);
            ArgumentNullException.ThrowIfNull(flowComponents);
            ArgumentNullException.ThrowIfNull(compositionMatrix);
            ArgumentNullException.ThrowIfNull(decomposition);

            Gases = gases;
            FlowComponents = flowComponents;
            CompositionMatrix = compositionMatrix;
            Decomposition = decomposition;
        }

        /// <summary>
        /// Fixed row ordering used by the matrix and target vector.
        /// </summary>
        public IReadOnlyList<string> Gases { get; }

        /// <summary>
        /// Fixed column ordering used by the matrix and solution vector.
        /// </summary>
        public IReadOnlyList<FlowComponent> FlowComponents { get; }

        public Matrix<double> CompositionMatrix { get; }

        public Svd<double> Decomposition { get; }
    }
}
