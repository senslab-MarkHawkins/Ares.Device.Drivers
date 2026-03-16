using System.Globalization;

namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class PhaseResponseParser : ResponseParser<PhaseNumberResponse>
{
  public PhaseResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out PhaseNumberResponse? response)
  {
    if(!int.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var phase))
    {
      response = null;
      return false;
    }

    response = new PhaseNumberResponse(address, status, phase);
    return true;
  }
}
