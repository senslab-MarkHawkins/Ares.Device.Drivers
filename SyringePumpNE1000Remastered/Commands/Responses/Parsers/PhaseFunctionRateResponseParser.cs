namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class PhaseFunctionRateResponseParser : ResponseParser<PhaseFunctionRateResponse>
{
  public PhaseFunctionRateResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out PhaseFunctionRateResponse? response)
  {
    if(content.Length < 3)
    {
      response = null;
      return false;
    }

    var unitStr = content[^2..];
    var floatStr = content[..^2];
    if(!Enum.TryParse<RateUnit>(unitStr, true, out var pumpRateUnit) || !TryParseFloat(floatStr, out var rateValue))
    {
      response = null;
      return false;
    }

    response = new PhaseFunctionRateResponse(address, status, rateValue, pumpRateUnit);
    return true;
  }
}
