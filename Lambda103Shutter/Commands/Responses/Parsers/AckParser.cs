using Ares.Toolkit.Serial.Commands;
using Lambda103Shutter.Commands.Responses;

namespace Lambda103Shutter.Commands.Responses.Parsers;

public class AckParser : SerialResponseParser<AckResponse>
{
    public override bool TryParseResponse(byte[] data, out AckResponse? response, out ArraySegment<byte>? remaining)
    {
        if (data.Length > 0)
        {
            response = new AckResponse();
            remaining = new ArraySegment<byte>(data, 1, data.Length - 1);
            return true;
        }

        response = null;
        remaining = null;
        return false;
    }
}
