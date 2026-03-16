using Ares.Toolkit.Serial.Commands;

namespace SyringePumpNE1000Remastered.Commands.Responses;

public class Response : SerialResponse
{
  public Response(int address, StatusPrompt status, CommandError? error = null)
  {
    Address = address;
    Status = status;
    Error = error;
  }

  public int Address { get; }
  public StatusPrompt Status { get; }
  public CommandError? Error { get; }
}
