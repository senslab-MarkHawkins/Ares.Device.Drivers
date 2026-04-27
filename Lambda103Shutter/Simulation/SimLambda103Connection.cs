using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.IO.Ports;

namespace Lambda103Shutter.Simulation;

public class SimLambda103Connection : AresSerialSimConnection
{
    private ILambda103Sim? _sim;

    public SimLambda103Connection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(50) })
    {
        _sim = new Lambda103Sim(AddDataReceived);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        _sim?.Dispose();
    }

    public override void SendInternally(byte[] bytes)
    {
        _sim?.SendCommand(bytes);
    }
}
