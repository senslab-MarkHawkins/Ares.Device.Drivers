using Ares.Toolkit.Serial.Commands;
using ShawHygrometer.Commands.Responses;

namespace ShawHygrometer.Commands.Responses.Parsers;

public class WaterPpmParser : SerialResponseParser<WaterPpmResponse>
{
    public override bool TryParseResponse(byte[] bytes, out WaterPpmResponse? response, out ArraySegment<byte>? consumed)
    {
        response = null;
        consumed = null;

        // Header: 255, 255, 255, 255, 255 (5 bytes)
        // Response length: 16 bytes
        // Value: bytes[11], bytes[12], bytes[13], bytes[14]
        
        if (bytes.Length < 16)
        {
            return false;
        }

        // Search for header in case of misalignment
        int headerIndex = -1;
        for (int i = 0; i <= bytes.Length - 16; i++)
        {
            if (bytes[i] == 255 && bytes[i+1] == 255 && bytes[i+2] == 255 && bytes[i+3] == 255 && bytes[i+4] == 255)
            {
                headerIndex = i;
                break;
            }
        }

        if (headerIndex == -1)
        {
            // If we have at least some data but no header, we might want to consume some bytes to keep buffer clean
            // For simplicity, let's just return false.
            return false;
        }

        try
        {
            byte[] valueBytes;
            if (BitConverter.IsLittleEndian)
            {
                valueBytes = new byte[] { bytes[headerIndex + 14], bytes[headerIndex + 13], bytes[headerIndex + 12], bytes[headerIndex + 11] };
            }
            else
            {
                valueBytes = new byte[] { bytes[headerIndex + 11], bytes[headerIndex + 12], bytes[headerIndex + 13], bytes[headerIndex + 14] };
            }

            float reading = BitConverter.ToSingle(valueBytes, 0);
            response = new WaterPpmResponse(reading);
            consumed = new ArraySegment<byte>(bytes, 0, headerIndex + 16);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
