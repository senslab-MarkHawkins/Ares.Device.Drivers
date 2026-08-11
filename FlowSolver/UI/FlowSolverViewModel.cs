using Ares.Toolkit.Device.UI;

namespace FlowSolver.UI;

public sealed class FlowSolverViewModel :
    DeviceUnitControlViewModel<FlowSolver>
{
    public FlowSolverViewModel(
        FlowSolver solver,
        ILogger<FlowSolverViewModel> logger)
        : base(solver)
    {
        ViewType = typeof(FlowSolverView);
        DefaultWidth = 19;

        logger.LogInformation(
            "Flow solver view model initialized.");
    }
}