using AlicatBusControl.Calculations;

namespace AlicatBusControl
{
    public interface IAlicatBusController: IAsyncDisposable
    {
        IReadOnlyList<FlowComponent> FlowComponents { get; }
        double? TargetFlow { get; }                                     // Total flow of all components


        Task SetTargetComposition(string gas, double concentration);    // Used to set internal target composition for a specific gas component
        Task SetTargetFlow(double totalFlow);                           // Total flow of all components
        Task<bool> ApplyFlow();                                         // update mfc flows to match current target composition and flow

        Task<double>GetTargetComposition(string gas);                   // Used to get internal target composition for a specific gas component
        Task<double> GetTotalFlow();                                    // Total flow of all components
    }
}
