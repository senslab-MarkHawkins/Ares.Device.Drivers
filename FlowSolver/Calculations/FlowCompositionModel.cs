using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using FlowSolver.Models;

namespace FlowSolver.Calculations;

public sealed class FlowCompositionModel
{
    public FlowCompositionModel(
        IReadOnlyList<string> gases,
        IReadOnlyList<FlowComponent> flowComponents,
        Matrix<double> compositionMatrix,
        Svd<double> decomposition)
    {
        Gases = gases;
        FlowComponents = flowComponents;
        CompositionMatrix = compositionMatrix;
        Decomposition = decomposition;
    }

    public IReadOnlyList<string> Gases { get; }

    public IReadOnlyList<FlowComponent> FlowComponents { get; }

    public Matrix<double> CompositionMatrix { get; }

    public Svd<double> Decomposition { get; }
}