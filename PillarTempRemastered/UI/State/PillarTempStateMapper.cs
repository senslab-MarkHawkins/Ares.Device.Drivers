using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace PillarTempRemastered.UI.State;

public static class PillarTempStateMapper
{
    public static PillarTempState FromAresStruct(AresStruct state)
    {
        return new PillarTempState
        {
            TargetTemperature = state.Fields.GetValueOrDefault("TargetTemperature")?.NumberValue ?? 0,
            CalculatedOutput = state.Fields.GetValueOrDefault("CalculatedOutput")?.NumberValue ?? 0,
            Status = state.Fields.GetValueOrDefault("Status")?.StringValue ?? "INACTIVE",
            ProportionalContribution = state.Fields.GetValueOrDefault("ProportionalContribution")?.NumberValue ?? 0,
            IntegralContribution = state.Fields.GetValueOrDefault("IntegralContribution")?.NumberValue ?? 0,
            DerivativeContribution = state.Fields.GetValueOrDefault("DerivativeContribution")?.NumberValue ?? 0
        };
    }
}
