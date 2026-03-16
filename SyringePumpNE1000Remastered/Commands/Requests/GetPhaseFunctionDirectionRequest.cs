using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class GetPhaseFunctionDirectionRequest : RequestExpectingResponse<PhaseFunctionDirectionResponse>
{
  public GetPhaseFunctionDirectionRequest(int address) : base(new PhaseFunctionDirectionResponseParser(address))
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Dir.ToProtocolString()}";
}
