namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class PhaseFunctionRateResponse : Response
{
  public PhaseFunctionRateResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public PhaseFunctionRateResponse(int address, StatusPrompt status, double rate, RateUnit rateUnit) : base(address, status)
  {
    Rate = rate;
    RateUnit = rateUnit;
  }

  public double Rate { get; }
  public RateUnit RateUnit { get; } = RateUnit.UndefinedRateUnit;
}
