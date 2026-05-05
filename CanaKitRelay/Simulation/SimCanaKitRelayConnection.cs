using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using System.IO.Ports;

namespace CanaKitRelay.Simulation;

public class SimCanaKitRelayConnection : AresSerialSimConnection
{
    private readonly RelaySim _sim;

    public SimCanaKitRelayConnection(string portName) : base(new SerialPortConnectionInfo(0, Parity.None, 0, StopBits.None), portName, new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(50) })
    {
        _sim = new RelaySim(AddDataReceived);
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
