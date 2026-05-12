using Ares.Toolkit.Serial.Commands;
using System.Text.RegularExpressions;

namespace MKS651DRemastered.Commands.Responses.Parsers;

public class PressureParser : AsciiResponseParser<MKS651DNumericResponse>
{
    protected override bool TryParseResponse(string line, out MKS651DNumericResponse? response)
    {
        // Expected: P+075.88\r or P-075.88\r
        if (line.StartsWith("P"))
        {
            var valueStr = line.Substring(1).Trim();
            if (double.TryParse(valueStr, out var value))
            {
                response = new MKS651DNumericResponse(line, value);
                return true;
            }
        }

        response = null;
        return false;
    }
}

public class GenericNumericParser : AsciiResponseParser<MKS651DNumericResponse>
{
    private readonly string _prefix;

    public GenericNumericParser(string prefix = "AA")
    {
        _prefix = prefix;
    }

    protected override bool TryParseResponse(string line, out MKS651DNumericResponse? response)
    {
        if (line.StartsWith(_prefix))
        {
            var valueStr = line.Substring(_prefix.Length).Trim();
            if (double.TryParse(valueStr, out var value))
            {
                response = new MKS651DNumericResponse(line, value);
                return true;
            }
        }

        response = null;
        return false;
    }
}

public class AckParser : AsciiResponseParser<MKS651DResponse>
{
    protected override bool TryParseResponse(string line, out MKS651DResponse? response)
    {
        // Most set commands don't seem to return a specific "OK", 
        // but legacy code just writes and then calls a getter.
        // We'll just return the line as is.
        response = new MKS651DResponse(line);
        return true;
    }
}
