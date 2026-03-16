using System.Globalization;

namespace SyringePumpNE1000Remastered.Commands;

internal static class FormatHelper
{
  private const string ReferenceFloatString = "####.";

  public static string FormatToFloatString(double input)
  {
    var inputMod = Math.Round(input, 3);
    var floatStr = inputMod.ToString("0.###", CultureInfo.InvariantCulture);
    if(!floatStr.Contains('.'))
      floatStr += ".0";

    if(floatStr.Length <= ReferenceFloatString.Length)
      return floatStr;

    var decimalIndex = floatStr.IndexOf('.');
    if(decimalIndex >= ReferenceFloatString.Length - 1)
      throw new InvalidOperationException($"Value '{input}' is out of range for NE1000 command formatting.");

    var decimalsToTruncate = floatStr.Length - ReferenceFloatString.Length;
    var decimalsToRound = 3 - decimalsToTruncate;
    inputMod = Math.Round(inputMod, decimalsToRound);
    return inputMod.ToString("0.###", CultureInfo.InvariantCulture);
  }

  public static string ToProtocolString(this Direction direction)
    => direction.ToString().ToUpperInvariant();

  public static string ToProtocolString(this RateUnit rateUnit)
    => rateUnit.ToString().ToUpperInvariant();

  public static string ToProtocolString(this VolumeUnit volumeUnit)
    => volumeUnit.ToString().ToUpperInvariant();

  public static string ToProtocolString(this SyringePumpFunction function)
    => function.ToString().ToUpperInvariant();
}
