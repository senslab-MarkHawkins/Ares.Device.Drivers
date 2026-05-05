using Ares.Toolkit.Serial.Commands;
using VacuumPumpPlugin.Commands.Responses;
using VacuumPumpPlugin.Commands.Responses.Parsers;

namespace VacuumPumpPlugin.Commands.Requests;

public class GetPumpStatusRequest : SerialCommandWithResponse<PumpStatusResponse>
{
  public GetPumpStatusRequest() : base(new PumpStatusParser())
  {
  }

  protected override byte[] Serialize()
  {
    return new byte[] { 0x02, 0x80, (byte)'2', (byte)'0', (byte)'5', 0x30, 0x03, (byte)'8', (byte)'6' };
  }
}
