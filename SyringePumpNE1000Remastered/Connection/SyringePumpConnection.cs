using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace SyringePumpNE1000Remastered.Connection;

public sealed class SyringePumpConnection : AresHardwareConnection, ISyringePumpConnection
{
  public SyringePumpConnection(string portName)
    : base(
      new SerialPortConnectionInfo(19200, Parity.None, 8, StopBits.One),
      portName,
      new SerialConnectionOptions
      {
        SendBuffer = TimeSpan.FromMilliseconds(50)
      })
  {
    AttemptOpen();
  }
}
