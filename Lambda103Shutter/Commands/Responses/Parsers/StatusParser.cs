using Ares.Toolkit.Serial.Commands;
using Lambda103Shutter.Commands.Responses;

namespace Lambda103Shutter.Commands.Responses.Parsers;

public class StatusParser : SerialResponseParser<StatusResponse>
{
    public override bool TryParseResponse(byte[] data, out StatusResponse? response, out ArraySegment<byte>? remaining)
    {
        if (data.Length < 6)
        {
            response = null;
            remaining = null;
            return false;
        }

        if (data[0] != 204)
        {
            response = null;
            remaining = new ArraySegment<byte>(data, 1, data.Length - 1);
            return false;
        }

        var filterByte = data[1];
        var filterWheel = filterByte - 112; 
        
        var shutterByte = data[5];
        var shutterOpen = shutterByte == 170;

        response = new StatusResponse(filterWheel, shutterOpen);
        remaining = new ArraySegment<byte>(data, 6, data.Length - 6);
        return true;
    }
}
