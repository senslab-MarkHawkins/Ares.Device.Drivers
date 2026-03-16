namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class PhaseFunctionResponse : Response
{
  public PhaseFunctionResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public PhaseFunctionResponse(int address, StatusPrompt status, SyringePumpFunction function) : base(address, status)
  {
    Function = function;
  }

  public SyringePumpFunction Function { get; } = SyringePumpFunction.UndefinedFunction;
}
