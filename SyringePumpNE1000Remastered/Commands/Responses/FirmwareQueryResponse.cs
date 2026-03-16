namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class FirmwareQueryResponse : Response
{
  public FirmwareQueryResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public FirmwareQueryResponse(int address, StatusPrompt status, string firmwareVersion) : base(address, status)
  {
    FirmwareVersion = firmwareVersion;
  }

  public string FirmwareVersion { get; } = string.Empty;
}
