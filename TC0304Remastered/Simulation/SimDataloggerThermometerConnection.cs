using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.IO.Ports;
using TC0304Remastered.Connection;

namespace TC0304Remastered.Simulation;

public class SimDataloggerThermometerConnection : AresSerialSimConnection, IDataloggerThermometerConnection
{
  private readonly SimulatedDataLogger _dataLogger;

  public SimDataloggerThermometerConnection(string portName)
    : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName)
  {
    _dataLogger = new SimulatedDataLogger(AddDataReceived);
  }

  public override void SendInternally(byte[] bytes)
  {
    _dataLogger.SendCommand(bytes);
  }

  public override async ValueTask DisposeAsync()
  {
    _dataLogger.Dispose();
    await base.DisposeAsync();
  }
}
