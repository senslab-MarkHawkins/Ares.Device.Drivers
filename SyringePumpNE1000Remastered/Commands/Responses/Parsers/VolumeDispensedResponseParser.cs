namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

internal sealed class VolumeDispensedResponseParser : ResponseParser<VolumeDispensedResponse>
{
  public VolumeDispensedResponseParser(int address) : base(address)
  {
  }

  protected override bool TryParseContentResponse(int address, StatusPrompt status, string content, out VolumeDispensedResponse? response)
  {
    if(content.Length < 8 || content[0] != 'I')
    {
      response = null;
      return false;
    }

    var wIndex = content.IndexOf('W');
    if(wIndex < 0 || content.Length <= wIndex + 3)
    {
      response = null;
      return false;
    }

    var unitStr = content[^2..];
    var infusedStr = content[1..wIndex];
    var withdrawnStr = content[(wIndex + 1)..^2];
    if(!Enum.TryParse<VolumeUnit>(unitStr, true, out var volumeUnit) ||
       !TryParseFloat(infusedStr, out var infusedRaw) ||
       !TryParseFloat(withdrawnStr, out var withdrawnRaw))
    {
      response = null;
      return false;
    }

    response = new VolumeDispensedResponse(address, status, infusedRaw, withdrawnRaw, volumeUnit);
    return true;
  }
}
