using Ares.Toolkit.Serial.Commands;
using System.Text;
using VacuumPumpPlugin.Commands.Responses;

namespace VacuumPumpPlugin.Commands.Responses.Parsers;

public class RotationSpeedParser : SerialResponseParser<RotationSpeedResponse>
{
  public override bool TryParseResponse(byte[] responseData, out RotationSpeedResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    if (responseData.Length < 4)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    try
    {
      // Legacy code: var rotationString = response.Substring(0, 4); RotationSpeed = int.Parse(rotationString);
      var rotationString = Encoding.ASCII.GetString(responseData, 0, 4);
      if (int.TryParse(rotationString, out var speed))
      {
        response = new RotationSpeedResponse(speed);
        dataToRemove = responseData[..4];
        return true;
      }
    }
    catch
    {
    }

    response = null;
    dataToRemove = null;
    return false;
  }
}
