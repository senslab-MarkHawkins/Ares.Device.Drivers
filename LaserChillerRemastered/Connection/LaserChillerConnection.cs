using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace LaserChillerRemastered.Connection;

public class LaserChillerConnection : AresHardwareConnection, ILaserChillerConnection
{
  public LaserChillerConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
  {
    AttemptOpen();
  }
}
