using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using SyringePumpNE1000Remastered.Connection;
using System.IO.Ports;

namespace SyringePumpNE1000Remastered.Simulation;

public sealed class SimSyringePumpConnection : AresSerialSimConnection, ISyringePumpConnection
{
  private readonly SimSyringePump _syringePump;

  public SimSyringePumpConnection(string portName, int address)
    : base(
      new SerialPortConnectionInfo(19200, Parity.None, 8, StopBits.One),
      portName,
      new SerialConnectionOptions
      {
        SendBuffer = TimeSpan.FromMilliseconds(10)
      })
  {
    _syringePump = new SimSyringePump(AddDataReceived, address);
  }

  public override void SendInternally(byte[] bytes)
    => _syringePump.SendCommand(bytes);
}
