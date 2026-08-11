namespace FlowSolver
{
    public interface IFlowSolver: IAsyncDisposable
    {
        Task SetTargetComponent(string gas, double? concentration, string unit);
        Task SetTargetFlow(double flow);
        Task CalculateSetpoints();
        Task<double> GetSetpoint(string mfc);
        Task ClearTarget();

    }
}
