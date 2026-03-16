using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class StartRequest : RequestExpectingResponse<Response>
{
  public StartRequest(int address) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Run.ToProtocolString()}";
}
