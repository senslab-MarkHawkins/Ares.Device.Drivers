using Ares.Toolkit.Serial.Commands;
using Lambda103Shutter.Commands.Responses;

namespace Lambda103Shutter.Commands.Responses.Parsers;

public class ValidationParser : SerialResponseParser<ValidationResponse>
{
    public override bool TryParseResponse(byte[] data, out ValidationResponse? response, out ArraySegment<byte>? remaining)
    {
        if (data.Length >= 29)
        {
            response = new ValidationResponse(data);
            remaining = new ArraySegment<byte>(Array.Empty<byte>());
            return true;
        }

        response = null;
        remaining = null;
        return false;
    }
}
