namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class AddressQueryResponse : Response
{
  public AddressQueryResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public AddressQueryResponse(int address, StatusPrompt status, int respondingAddress) : base(address, status)
  {
    RespondingAddress = respondingAddress;
  }

  public int RespondingAddress { get; }
}
