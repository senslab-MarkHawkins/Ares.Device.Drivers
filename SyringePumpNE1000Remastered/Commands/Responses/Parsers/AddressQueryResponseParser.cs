using System.Globalization;

namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class AddressQueryResponseParser : ResponseParser<AddressQueryResponse>
{
  public AddressQueryResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out AddressQueryResponse? response)
  {
    if(content.Length != 2 || !int.TryParse(content, NumberStyles.Integer, CultureInfo.InvariantCulture, out var respondingPumpAddress))
    {
      response = null;
      return false;
    }

    response = new AddressQueryResponse(address, status, respondingPumpAddress);
    return true;
  }
}
