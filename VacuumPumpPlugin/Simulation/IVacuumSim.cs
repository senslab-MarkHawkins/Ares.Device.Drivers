namespace VacuumPumpPlugin.Simulation;

public interface IVacuumSim : IDisposable
{
  string PortName { get; }
  void ProcessCommand(byte[] command);
}
