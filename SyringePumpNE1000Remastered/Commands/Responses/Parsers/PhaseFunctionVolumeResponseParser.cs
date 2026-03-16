namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class PhaseFunctionVolumeResponseParser : ResponseParser<PhaseFunctionVolumeResponse>
{
  public PhaseFunctionVolumeResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out PhaseFunctionVolumeResponse? response)
  {
    if(content.Length < 3)
    {
      response = null;
      return false;
    }

    var unitStr = content[^2..];
    var volumeStr = content[..^2];
    if(!Enum.TryParse<VolumeUnit>(unitStr, true, out var pumpVolumeUnit) || !TryParseFloat(volumeStr, out var volumeValue))
    {
      response = null;
      return false;
    }

    response = new PhaseFunctionVolumeResponse(address, status, volumeValue, pumpVolumeUnit);
    return true;
  }
}
