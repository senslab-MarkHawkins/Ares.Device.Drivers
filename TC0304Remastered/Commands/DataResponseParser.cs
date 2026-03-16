using Ares.Toolkit.Serial.Commands;
using UnitsNet;

namespace TC0304Remastered.Commands;

internal class DataResponseParser : SerialResponseParser<DataResponse>
{
  private const byte FirstByte = 2;
  private const byte LastByte = 3;
  private const int ResponseLength = 45;

  private const byte BatteryLowMask = 0b_0100_0001;
  private const byte CelsiusMask = 0b_1000_0000;
  private const byte ModeMask = 0b_0000_0110;
  private const byte T1T2Mask = 0b_0000_1000;
  private const byte HoldMask = 0b_0010_0000;

  public override bool TryParseResponse(byte[] buffer, out DataResponse? response, out ArraySegment<byte>? dataToRemove)
  {
    var bufferArray = buffer.ToArray();
    if(bufferArray.Length < ResponseLength)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    var firstOfLine = Array.IndexOf(bufferArray, FirstByte);
    var lastOfLine = Array.IndexOf(bufferArray, LastByte, ResponseLength - 1);
    if(firstOfLine == -1 || lastOfLine == -1)
    {
      response = null;
      dataToRemove = null;
      return false;
    }

    if(lastOfLine - firstOfLine + 1 != ResponseLength)
      throw new InvalidOperationException($"Malformed response from TC0304{Environment.NewLine}{string.Join(" ", bufferArray[firstOfLine..lastOfLine].Select(b => b.ToString("X")))}");

    dataToRemove = new ArraySegment<byte>(bufferArray, firstOfLine, ResponseLength);
    return TryParseLine(dataToRemove.Value, out response);
  }

  private static bool TryParseLine(ArraySegment<byte> bytes, out DataResponse? response)
  {
    var lineArr = bytes.ToArray();
    var infoByte = bytes[1];
    var batteryLow = (infoByte & BatteryLowMask) == BatteryLowMask;
    var mode = ModeExtensions.FromInt((infoByte & ModeMask) >> 1);
    var celsius = (infoByte & CelsiusMask) == CelsiusMask;
    var hold = (infoByte & HoldMask) == HoldMask;
    var t1T2 = (infoByte & T1T2Mask) == T1T2Mask;

    response = new DataResponse
    {
      BatteryLow = batteryLow,
      Hold = hold,
      Celsius = celsius,
      Mode = mode,
      T1Probe = GetTemperature(lineArr[7..9], celsius),
      T2Probe = GetTemperature(lineArr[9..11], celsius),
      T3Probe = GetTemperature(lineArr[11..13], celsius),
      T4Probe = GetTemperature(lineArr[13..15], celsius),
      T1T2 = t1T2
    };

    return true;
  }

  private static Temperature? GetTemperature(byte[] bytes, bool celsius)
  {
    var value = (bytes[0] << 8) | bytes[1];
    if(value == 0x7FFF)
      return null;

    return celsius ? Temperature.FromDegreesCelsius((double)value / 10) : Temperature.FromDegreesFahrenheit((double)value / 10);
  }
}
