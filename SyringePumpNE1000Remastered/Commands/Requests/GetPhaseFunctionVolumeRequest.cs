using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class GetPhaseFunctionVolumeRequest : RequestExpectingResponse<PhaseFunctionVolumeResponse>
{
  public GetPhaseFunctionVolumeRequest(int address) : base(new PhaseFunctionVolumeResponseParser(address))
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Vol.ToProtocolString()}";
}
