using Ares.Toolkit.Serial.Commands;
using VacuumGaugeController.Commands.Responses;

namespace VacuumGaugeController.Commands.Responses.Parsers;

public class AckResponse : VacuumGaugeResponse
{
}

public class AckParser : AsciiResponseParser<AckResponse>
{
    protected override bool TryParseResponse(string line, out AckResponse? response)
    {
        response = null;
        if (line.Contains("\u0006"))
        {
            response = new AckResponse();
            return true;
        }
        return false;
    }
}
