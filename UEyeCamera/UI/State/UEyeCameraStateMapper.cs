using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace UEyeCamera.UI.State;

public static class UEyeCameraStateMapper
{
    public static UEyeCameraState FromAresStruct(AresStruct state)
    {
        return new UEyeCameraState
        {
            FPS = state.Fields.GetValueOrDefault("FPS")?.NumberValue ?? 0,
            CaptureStatus = (int)(state.Fields.GetValueOrDefault("CaptureStatus")?.NumberValue ?? 0),
            IsOpened = state.Fields.GetValueOrDefault("IsOpened")?.BoolValue ?? false
        };
    }
}
