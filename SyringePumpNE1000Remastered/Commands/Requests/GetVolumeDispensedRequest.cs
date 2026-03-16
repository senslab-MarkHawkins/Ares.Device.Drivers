using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class GetVolumeDispensedRequest : RequestExpectingResponse<VolumeDispensedResponse>
{
  public GetVolumeDispensedRequest(int address) : base(new VolumeDispensedResponseParser(address))
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Dis.ToProtocolString()}";
}
