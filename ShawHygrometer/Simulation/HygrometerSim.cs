using ShawHygrometer.Commands.Responses;

namespace ShawHygrometer.Simulation;

public class HygrometerSim : IDisposable
{
    private readonly Action<byte[]> _byteSender;
    private readonly Random _random = new();

    public HygrometerSim(Action<byte[]> byteSender)
    {
        _byteSender = byteSender;
    }

    public void ProcessCommand(byte[] bytes)
    {
        // GetWaterPpm command: 0xff, 0xff, 0xff, 0xff, 0xff, 0x02, 0x00, 24, 0x01, 0x00, 27
        if (bytes.Length == 11 && bytes[0] == 0xff && bytes[5] == 0x02)
        {
            var ppm = (float)(_random.NextDouble() * 100);
            var response = new byte[16];
            // Header
            for (int i = 0; i < 5; i++) response[i] = 0xff;
            
            // Value at bytes 11-14
            byte[] valueBytes = BitConverter.GetBytes(ppm);
            if (BitConverter.IsLittleEndian)
            {
                response[11] = valueBytes[3];
                response[12] = valueBytes[2];
                response[13] = valueBytes[1];
                response[14] = valueBytes[0];
            }
            else
            {
                response[11] = valueBytes[0];
                response[12] = valueBytes[1];
                response[13] = valueBytes[2];
                response[14] = valueBytes[3];
            }
            
            _byteSender(response);
        }
    }

    public void Dispose()
    {
    }
}
