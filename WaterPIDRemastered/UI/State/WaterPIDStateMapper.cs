using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace WaterPIDRemastered.UI.State;

public static class WaterPIDStateMapper
{
    public static WaterPIDState FromAresStruct(AresStruct state)
    {
        return new WaterPIDState
        {
            TargetPPM = state.Fields.GetValueOrDefault("TargetPPM")?.NumberValue ?? 0,
            CalculatedOutput = state.Fields.GetValueOrDefault("CalculatedOutput")?.NumberValue ?? 0,
            Status = state.Fields.GetValueOrDefault("Status")?.StringValue ?? "INACTIVE",
            ProportionalContribution = state.Fields.GetValueOrDefault("ProportionalContribution")?.NumberValue ?? 0,
            IntegralContribution = state.Fields.GetValueOrDefault("IntegralContribution")?.NumberValue ?? 0,
            DerivativeContribution = state.Fields.GetValueOrDefault("DerivativeContribution")?.NumberValue ?? 0
        };
    }
}
