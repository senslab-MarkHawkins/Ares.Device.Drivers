using Ares.Toolkit.Serial.Commands;
using System.Text;
using VacuumPumpPlugin.Commands.Responses;

namespace VacuumPumpPlugin.Commands.Responses.Parsers;

public class TestResponseParser : SerialResponseParser<TestResponse>
{
  public override bool TryParseResponse(byte[] responseData, out TestResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    response = new TestResponse(Encoding.ASCII.GetString(responseData));
    dataToRemove = responseData;
    return true;
  }
}
