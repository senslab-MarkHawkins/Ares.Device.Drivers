using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class QueryPhaseFunctionRequest : RequestExpectingResponse<PhaseFunctionResponse>
{
  public QueryPhaseFunctionRequest(int address) : base(new PhaseFunctionResponseParser(address))
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Fun.ToProtocolString()}";
}
