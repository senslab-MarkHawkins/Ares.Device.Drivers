using Ares.Toolkit.Serial.Commands;
using CanaKitRelay.Commands.Responses;
using System.Text.RegularExpressions;

namespace CanaKitRelay.Commands.Responses.Parsers;

public class RelayResponseParser<T> : AsciiResponseParser<T> where T : CanaKitRelayResponse
{
    private static readonly Regex _relayStateRegex = new(@"REL(\d)=(0|1)", RegexOptions.Compiled);

    protected override bool TryParseResponse(string line, out T? response)
    {
        var match = _relayStateRegex.Match(line);
        if (match.Success)
        {
            var relayNum = int.Parse(match.Groups[1].Value);
            var state = match.Groups[2].Value == "1";
            response = new RelayStateResponse(relayNum, state) as T;
            return response != null;
        }

        if (line.Contains("::"))
        {
            response = new GenericRelayResponse() as T;
            return response != null;
        }

        response = null;
        return false;
    }
}
