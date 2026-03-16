namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class PhaseNumberResponse : Response
{
  public PhaseNumberResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public PhaseNumberResponse(int address, StatusPrompt status, int phase) : base(address, status)
  {
    Phase = phase;
  }

  public int Phase { get; }
}
