using Ares.Toolkit.Serial;
using System.IO.Ports;

namespace MKS651DRemastered.Connection;

public interface IMKS651DConnection : IAresSerialConnection
{
}

public class MKS651DConnection : AresHardwareConnection, IMKS651DConnection
{
    public MKS651DConnection(string portName) : base(
        new SerialPortConnectionInfo(9600, Parity.None, 8, StopBits.One), 
        portName,
        new SerialConnectionOptions
        {
            SendBuffer = TimeSpan.FromMilliseconds(50),
            SendTimeout = TimeSpan.FromSeconds(2)
        }
    )
    {
        AttemptOpen();
    }
}
