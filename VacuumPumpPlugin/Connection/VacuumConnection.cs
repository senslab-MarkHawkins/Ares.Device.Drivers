using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace VacuumPumpPlugin.Connection;

public class VacuumConnection : AresHardwareConnection, IVacuumConnection
{
  public VacuumConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName,
    new SerialConnectionOptions
    {
      SendBuffer = TimeSpan.FromMilliseconds(50),
      SendTimeout = TimeSpan.FromSeconds(2)
    }
  )
  {
  }
}
