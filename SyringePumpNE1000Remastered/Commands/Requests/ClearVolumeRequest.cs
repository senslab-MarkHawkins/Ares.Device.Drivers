using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class ClearVolumeRequest : RequestExpectingResponse<Response>
{
  public ClearVolumeRequest(int address, Direction direction) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Direction = direction;
  }

  public int Address { get; }
  public Direction Direction { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Cld.ToProtocolString()} {Direction.ToProtocolString()}";
}
