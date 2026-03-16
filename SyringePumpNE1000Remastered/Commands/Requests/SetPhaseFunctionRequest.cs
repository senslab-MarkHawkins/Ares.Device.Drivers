using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class SetPhaseFunctionRequest : RequestExpectingResponse<Response>
{
  public SetPhaseFunctionRequest(int address, SyringePumpFunction function) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Function = function;
  }

  public int Address { get; }
  public SyringePumpFunction Function { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Fun.ToProtocolString()} {Function.ToProtocolString()}";
}
