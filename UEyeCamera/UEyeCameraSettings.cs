using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace UEyeCamera;

public class UEyeCameraSettings
{
    public const string CameraIdKey = "CameraId";
    public const string GainKey = "Gain";
    public const string ExposureKey = "Exposure";
    public const string PixelClockKey = "PixelClock";
    public const string AutoWhiteBalanceKey = "AutoWhiteBalance";
    public const string AutoGainKey = "AutoGain";

    public int CameraId { get; set; } = 1;
    public int Gain { get; set; } = 0;
    public double Exposure { get; set; } = 10.0;
    public int PixelClock { get; set; } = 30;
    public bool AutoWhiteBalance { get; set; } = false;
    public bool AutoGain { get; set; } = false;

    public static UEyeCameraSettings FromStruct(AresStruct settings)
    {
        return new UEyeCameraSettings
        {
            CameraId = (int)(settings.Fields.GetValueOrDefault(CameraIdKey)?.NumberValue ?? 1),
            Gain = (int)(settings.Fields.GetValueOrDefault(GainKey)?.NumberValue ?? 0),
            Exposure = settings.Fields.GetValueOrDefault(ExposureKey)?.NumberValue ?? 10.0,
            PixelClock = (int)(settings.Fields.GetValueOrDefault(PixelClockKey)?.NumberValue ?? 30),
            AutoWhiteBalance = settings.Fields.GetValueOrDefault(AutoWhiteBalanceKey)?.BoolValue ?? false,
            AutoGain = settings.Fields.GetValueOrDefault(AutoGainKey)?.BoolValue ?? false
        };
    }

    public AresStruct ToStruct()
    {
        return AresStructHelper.CreateNumberStruct(CameraIdKey, CameraId)
            .AddNumber(GainKey, Gain)
            .AddNumber(ExposureKey, Exposure)
            .AddNumber(PixelClockKey, PixelClock)
            .AddBool(AutoWhiteBalanceKey, AutoWhiteBalance)
            .AddBool(AutoGainKey, AutoGain);
    }
}
