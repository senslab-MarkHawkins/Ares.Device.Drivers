using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class GetPhaseFunctionRateRequest : RequestExpectingResponse<PhaseFunctionRateResponse>
{
  public GetPhaseFunctionRateRequest(int address) : base(new PhaseFunctionRateResponseParser(address))
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Rat.ToProtocolString()}";
}
