namespace VacuumPumpPlugin.Commands.Responses;

public class TestResponse : VacuumResponse
{
  public TestResponse(string response)
  {
    Response = response;
  }

  public string Response { get; }
}
