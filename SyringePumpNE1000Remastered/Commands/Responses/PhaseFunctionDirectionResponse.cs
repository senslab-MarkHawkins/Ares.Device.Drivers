namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class PhaseFunctionDirectionResponse : Response
{
  public PhaseFunctionDirectionResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public PhaseFunctionDirectionResponse(int address, StatusPrompt status, Direction direction) : base(address, status)
  {
    Direction = direction;
  }

  public Direction Direction { get; } = Direction.UndefinedDirection;
}
