namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class FirmwareQueryResponseParser : ResponseParser<FirmwareQueryResponse>
{
  public FirmwareQueryResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out FirmwareQueryResponse? response)
  {
    response = new FirmwareQueryResponse(address, status, content);
    return true;
  }
}
