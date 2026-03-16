using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using LaserChillerRemastered.Connection;
using System.IO.Ports;

namespace LaserChillerRemastered.Simulation;

public class SimLaserChillerConnection : AresSerialSimConnection, ILaserChillerConnection
{
  private readonly SimLaserChiller _simLaserChiller;

  public SimLaserChillerConnection(string portName)
    : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
  {
    _simLaserChiller = new SimLaserChiller(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _simLaserChiller.SendCommand(bytes);
  }
}
