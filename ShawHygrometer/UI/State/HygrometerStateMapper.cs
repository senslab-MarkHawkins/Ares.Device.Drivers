using Ares.Datamodel;

namespace ShawHygrometer.UI.State;

public static class HygrometerStateMapper
{
    public static ShawSuperdewState FromShawAresStruct(AresStruct state)
    {
        return new ShawSuperdewState
        {
            WaterPpm = (float)(state.Fields.GetValueOrDefault("WaterPpm")?.NumberValue ?? 0)
        };
    }
}
