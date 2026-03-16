using Ares.Toolkit.Serial.Commands;

namespace LaserChillerRemastered.Commands.Requests;

public class SetStabilizedTemperatureCommand : SerialCommand
{
  private readonly double _stabilizedTemperature;

  public SetStabilizedTemperatureCommand(double stabilizedTemperature)
  {
    _stabilizedTemperature = stabilizedTemperature;
  }

  protected override byte[] Serialize()
  {
    var signPrefix = _stabilizedTemperature < 0 ? "-" : "+";
    var decimalFormattedTemp = $"{Math.Abs(_stabilizedTemperature):00.0}";
    var formattedTempStr = signPrefix + decimalFormattedTemp.Replace(".", string.Empty);

    var temperatureBytes = formattedTempStr.ToCharArray().Select(c => (byte)c).ToArray();
    var checkSumBytes = new byte[] { 0x2E, 0x4D, temperatureBytes[0], temperatureBytes[1], temperatureBytes[2], temperatureBytes[3] };
    var checkSum = CalculateCheckSum(checkSumBytes);

    return [0x2E, 0x4D, temperatureBytes[0], temperatureBytes[1], temperatureBytes[2], temperatureBytes[3], checkSum[0], checkSum[1], 0x0D];
  }

  private static byte[] CalculateCheckSum(byte[] data)
  {
    var sum = data.Aggregate((left, right) => (byte)(left + right));
    var sumHexStr = $"{sum:X2}";
    return sumHexStr.Select(character => (byte)character).ToArray();
  }
}
