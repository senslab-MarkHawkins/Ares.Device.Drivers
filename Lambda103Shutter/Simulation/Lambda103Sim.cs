using System.Text;

namespace Lambda103Shutter.Simulation;

public class Lambda103Sim : ILambda103Sim
{
    private readonly Action<byte[]> _byteSender;
    private bool _shutterOpen = false;
    private int _filterWheel = 0;

    public Lambda103Sim(Action<byte[]> byteSender)
    {
        _byteSender = byteSender;
    }

    public void Dispose()
    {
    }

    public async Task SendCommand(byte[] command)
    {
        if (command.Length == 0) return;

        await Task.Delay(10); // Sim latency

        if (command.Length == 1)
        {
            var cmd = command[0];
            if (cmd == 204) // GetStatus
            {
                var response = new byte[6];
                response[0] = 204; // Echo
                response[1] = (byte)(112 + _filterWheel); // Filter
                response[2] = 0;
                response[3] = 0;
                response[4] = 0;
                response[5] = _shutterOpen ? (byte)170 : (byte)172;
                _byteSender(response);
            }
            else if (cmd == 170) // Shutter Open
            {
                _shutterOpen = true;
                _byteSender(new byte[] { 63 }); // Ack with '?'
            }
            else if (cmd == 172) // Shutter Close
            {
                _shutterOpen = false;
                _byteSender(new byte[] { 63 }); // Ack with '?'
            }
            else if (cmd >= 112 && cmd <= 121) // Set Wheel
            {
                _filterWheel = cmd - 112;
                _byteSender(new byte[] { 63 }); // Ack with '?'
            }
        }
        else if (command.Length == 2 && command[0] == 238 && command[1] == 253) // Validation
        {
            // Send 33 bytes as mentioned in legacy code ("Length = 33 (in lab)")
            var response = new byte[33];
            new Random().NextBytes(response);
            _byteSender(response);
        }
    }
}
