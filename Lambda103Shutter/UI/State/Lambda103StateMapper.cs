using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace Lambda103Shutter.UI.State;

public static class Lambda103StateMapper
{
    public static Lambda103State FromAresStruct(AresStruct state)
    {
        return new Lambda103State
        {
            FilterWheel = (int)(state.Fields.GetValueOrDefault("FilterWheel")?.NumberValue ?? 0),
            ShutterOpen = state.Fields.GetValueOrDefault("ShutterOpen")?.BoolValue ?? false
        };
    }
}
