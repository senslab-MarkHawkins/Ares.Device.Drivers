using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class SetPhaseFunctionDirectionRequest : RequestExpectingResponse<Response>
{
  public SetPhaseFunctionDirectionRequest(int address, Direction direction) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Direction = direction;
  }

  public int Address { get; }
  public Direction Direction { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Dir.ToProtocolString()} {Direction.ToProtocolString()}";
}
