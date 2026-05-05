using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.IO.Ports;
using VacuumPumpPlugin.Connection;

namespace VacuumPumpPlugin.Simulation;

public class SimVacuumConnection : AresSerialSimConnection, IVacuumConnection
{
  private readonly IVacuumSim _sim;

  public SimVacuumConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(150) })
  {
    _sim = new VacuumSim(portName, AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _sim.ProcessCommand(bytes);
  }

  public override async ValueTask DisposeAsync()
  {
    await base.DisposeAsync();
    _sim.Dispose();
  }
}
