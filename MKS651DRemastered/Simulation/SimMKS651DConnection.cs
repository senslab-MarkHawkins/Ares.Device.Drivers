using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using MKS651DRemastered.Connection;
using System.IO.Ports;
using System.Text;

namespace MKS651DRemastered.Simulation;

public class SimMKS651DConnection : AresSerialSimConnection, IMKS651DConnection
{
    private readonly MKS651DSim _sim = new();

    public SimMKS651DConnection(string portName) : base(
        new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), 
        portName, 
        new SerialConnectionOptions { SendBuffer = TimeSpan.FromMilliseconds(50) })
    {
    }

    public override void SendInternally(byte[] bytes)
    {
        var command = Encoding.ASCII.GetString(bytes);
        var response = _sim.ProcessCommand(command);
        if (!string.IsNullOrEmpty(response))
        {
            AddDataReceived(Encoding.ASCII.GetBytes(response + "\r"));
        }
    }
}
