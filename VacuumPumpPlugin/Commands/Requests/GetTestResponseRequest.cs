using Ares.Toolkit.Serial.Commands;
using VacuumPumpPlugin.Commands.Responses;
using VacuumPumpPlugin.Commands.Responses.Parsers;

namespace VacuumPumpPlugin.Commands.Requests;

public class GetTestResponseRequest : SerialCommandWithResponse<TestResponse>
{
  public GetTestResponseRequest() : base(new TestResponseParser())
  {
  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0x02, 0x80, (byte)'0', (byte)'1', (byte)'0', 0x30, 0x03, (byte)'8', (byte)'2' };
  }
}
