namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class ConfirmationResponseParser : ResponseParser<Response>
{
  public ConfirmationResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out Response? response)
  {
    if(content.Length != 0)
    {
      response = null;
      return false;
    }

    response = new Response(address, status);
    return true;
  }
}
