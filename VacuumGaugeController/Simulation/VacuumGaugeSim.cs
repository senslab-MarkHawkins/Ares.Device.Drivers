using System.Text;
using VacuumGaugeController.Enums;

namespace VacuumGaugeController.Simulation;

public class VacuumGaugeSim : IDisposable
{
    private readonly Action<byte[]> _byteSender;
    private readonly Random _random = new();
    private string _lastCommand = "";

    public VacuumGaugeSim(Action<byte[]> byteSender)
    {
        _byteSender = byteSender;
    }

    public void ProcessCommand(byte[] bytes)
    {
        var command = Encoding.ASCII.GetString(bytes);

        if (command == "PR1\r\n")
        {
            _lastCommand = "PR1";
            _byteSender(new byte[] { 0x06 }); // ACK
        }
        else if (command == "ERR\r\n")
        {
            _lastCommand = "ERR";
            _byteSender(new byte[] { 0x06 }); // ACK
        }
        else if (bytes.Length == 1 && bytes[0] == 0x05) // ENQ
        {
            if (_lastCommand == "PR1")
            {
                var pressure = (float)(_random.NextDouble() * 1e-3);
                var status = VacuumGaugeControllerPressureStatus.Okay;
                // Format: s, pressure (sx.xxxxEsxx) -> 0, 1.2345E-03
                var response = $"{(int)status}, {pressure:E4}"; 
                // Ensure length is consistent with what parser expects
                _byteSender(Encoding.ASCII.GetBytes(response));
            }
            else if (_lastCommand == "ERR")
            {
                var error = VacuumGaugeControllerErrorStatus.NoError;
                var response = $"{(int)error:D4}";
                _byteSender(Encoding.ASCII.GetBytes(response));
            }
            _lastCommand = "";
        }
    }

    public void Dispose()
    {
    }
}
