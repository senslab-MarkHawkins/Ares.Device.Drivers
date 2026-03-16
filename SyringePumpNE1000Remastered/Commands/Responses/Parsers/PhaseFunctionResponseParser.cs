namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class PhaseFunctionResponseParser : ResponseParser<PhaseFunctionResponse>
{
  public PhaseFunctionResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out PhaseFunctionResponse? response)
  {
    if(!Enum.TryParse<SyringePumpFunction>(content, true, out var function))
    {
      response = null;
      return false;
    }

    response = new PhaseFunctionResponse(address, status, function);
    return true;
  }
}
