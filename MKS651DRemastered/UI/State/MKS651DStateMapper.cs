using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace MKS651DRemastered.UI.State;

public static class MKS651DStateMapper
{
    public static MKS651DState FromAresStruct(AresStruct state)
    {
        var model = new MKS651DState
        {
            Pressure = state.Fields.GetValueOrDefault("Pressure")?.NumberValue ?? 0,
            ValvePosition = state.Fields.GetValueOrDefault("ValvePosition")?.NumberValue ?? 0,
            ActiveSetpoint = (int)(state.Fields.GetValueOrDefault("ActiveSetpoint")?.NumberValue ?? 0)
        };

        if (state.Fields.TryGetValue("Setpoints", out var spListVal) && spListVal.ListValue != null)
        {
            model.Setpoints = spListVal.ListValue.Values
                .Where(v => v.StructValue != null)
                .Select(v => new SetpointState
                {
                    Index = (int)(v.StructValue!.Fields.GetValueOrDefault("Index")?.NumberValue ?? 0),
                    Pressure = v.StructValue.Fields.GetValueOrDefault("Pressure")?.NumberValue ?? 0,
                    Gain = v.StructValue.Fields.GetValueOrDefault("Gain")?.NumberValue ?? 0,
                    Soft = v.StructValue.Fields.GetValueOrDefault("Soft")?.NumberValue ?? 0
                }).ToList();
        }

        return model;
    }
}
