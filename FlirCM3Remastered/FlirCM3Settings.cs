using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace FlirCM3Remastered;

public sealed class FlirCM3Settings
{
  public const string ExposureCompensationKey = "ExposureCompensation";
  public const string ExposureTimeKey = "ExposureTime";
  public const string GainKey = "Gain";
  public const string GammaKey = "Gamma";
  public const string BlackLevelKey = "BlackLevel";
  public const string RedBalanceKey = "RedBalance";
  public const string BlueBalanceKey = "BlueBalance";
  public const string CaptureWidthKey = "CaptureWidth";
  public const string CaptureHeightKey = "CaptureHeight";
  public const string OffsetXKey = "OffsetX";
  public const string OffsetYKey = "OffsetY";

  public double ExposureCompensation { get; set; }
  public double ExposureTime { get; set; } = 30291;
  public double Gain { get; set; }
  public double Gamma { get; set; }
  public double BlackLevel { get; set; }
  public double RedBalance { get; set; } = 2;
  public double BlueBalance { get; set; } = 2;
  public long CaptureWidth { get; set; } = 1280;
  public long CaptureHeight { get; set; } = 1024;
  public long OffsetX { get; set; }
  public long OffsetY { get; set; }

  public static FlirCM3Settings FromStruct(AresStruct? settings)
  {
    var result = new FlirCM3Settings();
    if(settings is null)
      return result;

    result.ExposureCompensation = GetDouble(settings, ExposureCompensationKey, result.ExposureCompensation);
    result.ExposureTime = GetDouble(settings, ExposureTimeKey, result.ExposureTime);
    result.Gain = GetDouble(settings, GainKey, result.Gain);
    result.Gamma = GetDouble(settings, GammaKey, result.Gamma);
    result.BlackLevel = GetDouble(settings, BlackLevelKey, result.BlackLevel);
    result.RedBalance = GetDouble(settings, RedBalanceKey, result.RedBalance);
    result.BlueBalance = GetDouble(settings, BlueBalanceKey, result.BlueBalance);
    result.CaptureWidth = (long)GetDouble(settings, CaptureWidthKey, result.CaptureWidth);
    result.CaptureHeight = (long)GetDouble(settings, CaptureHeightKey, result.CaptureHeight);
    result.OffsetX = (long)GetDouble(settings, OffsetXKey, result.OffsetX);
    result.OffsetY = (long)GetDouble(settings, OffsetYKey, result.OffsetY);

    return result;
  }

  public AresStruct ToStruct()
  {
    return AresStructHelper.CreateNumberStruct(ExposureCompensationKey, ExposureCompensation)
      .AddNumber(ExposureTimeKey, ExposureTime)
      .AddNumber(GainKey, Gain)
      .AddNumber(GammaKey, Gamma)
      .AddNumber(BlackLevelKey, BlackLevel)
      .AddNumber(RedBalanceKey, RedBalance)
      .AddNumber(BlueBalanceKey, BlueBalance)
      .AddNumber(CaptureWidthKey, CaptureWidth)
      .AddNumber(CaptureHeightKey, CaptureHeight)
      .AddNumber(OffsetXKey, OffsetX)
      .AddNumber(OffsetYKey, OffsetY);
  }

  private static double GetDouble(AresStruct settings, string key, double fallback)
  {
    return settings.Fields.TryGetValue(key, out var value) && value.HasNumberValue
      ? value.NumberValue
      : fallback;
  }
}
