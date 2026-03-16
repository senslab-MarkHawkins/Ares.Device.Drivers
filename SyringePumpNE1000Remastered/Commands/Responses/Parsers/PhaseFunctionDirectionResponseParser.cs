namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class PhaseFunctionDirectionResponseParser : ResponseParser<PhaseFunctionDirectionResponse>
{
  public PhaseFunctionDirectionResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out PhaseFunctionDirectionResponse? response)
  {
    if(!Enum.TryParse<Direction>(content, true, out var direction))
    {
      response = null;
      return false;
    }

    response = new PhaseFunctionDirectionResponse(address, status, direction);
    return true;
  }
}
