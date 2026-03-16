using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class SetAddressRequest : RequestExpectingResponse<Response>
{
  public SetAddressRequest(int address) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"*{SyringePumpFunction.Adr.ToProtocolString()} {Address}";
}
