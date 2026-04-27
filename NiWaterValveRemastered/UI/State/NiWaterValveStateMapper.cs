using Ares.Datamodel;

namespace NiWaterValve.UI.State;

public static class NiWaterValveStateMapper
{
    public static NiWaterValveState FromAresStruct(AresStruct state)
    {
        return new NiWaterValveState
        {
            ValveVoltage = state.Fields.GetValueOrDefault("ValveVoltage")?.NumberValue ?? 0,
            WaterTarget = state.Fields.GetValueOrDefault("WaterTarget")?.NumberValue ?? 0
        };
    }
}
