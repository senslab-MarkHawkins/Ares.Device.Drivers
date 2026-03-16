using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class SetPhaseNumberRequest : RequestExpectingResponse<Response>
{
  public SetPhaseNumberRequest(int address, int phase) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Phase = phase;
  }

  public int Address { get; }
  public int Phase { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Phn.ToProtocolString()} {Phase}";
}
