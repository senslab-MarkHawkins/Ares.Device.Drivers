using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace TC0304Remastered.Connection;

public class DataloggerThermometerConnection : AresHardwareConnection, IDataloggerThermometerConnection
{
  public DataloggerThermometerConnection(string portName)
    : base(new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), portName)
  {
  }
}
