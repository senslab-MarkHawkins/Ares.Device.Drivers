namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class DiameterResponse : Response
{
  public DiameterResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public DiameterResponse(int address, StatusPrompt status, double diameterMm) : base(address, status)
  {
    DiameterMm = diameterMm;
  }

  public double DiameterMm { get; }
}
