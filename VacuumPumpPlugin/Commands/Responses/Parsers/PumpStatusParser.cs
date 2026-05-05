using Ares.Toolkit.Serial.Commands;
using VacuumPumpPlugin.Commands.Responses;
using VacuumPumpPlugin.Enums;

namespace VacuumPumpPlugin.Commands.Responses.Parsers;

public class PumpStatusParser : SerialResponseParser<PumpStatusResponse>
{
  public override bool TryParseResponse(byte[] responseData, out PumpStatusResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    if (responseData.Length < 6)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    try
    {
      // Legacy code: var formattedResponse = response[5]; var index = int.Parse($"{formattedResponse}");
      var statusByte = responseData[5];
      var index = int.Parse(((char)statusByte).ToString());
      response = new PumpStatusResponse((VacuumPumpStatus)index);
      dataToRemove = responseData[..5];
      return true;
    }
    catch
    {
      response = null;
      dataToRemove = null;
      return false;
    }
  }
}
