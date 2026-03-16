using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class GetAddressRequest : RequestExpectingResponse<AddressQueryResponse>
{
  public GetAddressRequest(int address) : base(new AddressQueryResponseParser(address), false)
  {
  }

  protected override string GenerateCommandString()
    => $"*{SyringePumpFunction.Adr.ToProtocolString()}";
}
