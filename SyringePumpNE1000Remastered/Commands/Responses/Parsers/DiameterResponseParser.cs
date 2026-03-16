namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class DiameterResponseParser : ResponseParser<DiameterResponse>
{
  public DiameterResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out DiameterResponse? response)
  {
    if(!TryParseFloat(content, out var diameterMm))
    {
      response = null;
      return false;
    }

    response = new DiameterResponse(address, status, diameterMm);
    return true;
  }
}
