namespace FlowSolver.Models;

public sealed record FlowSetpoint(
    string DeviceName,
    double Flow);