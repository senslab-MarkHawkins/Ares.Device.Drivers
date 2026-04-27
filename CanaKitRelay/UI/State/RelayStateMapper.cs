using Ares.Datamodel;
using Ares.Datamodel.Extensions;

namespace CanaKitRelay.UI.State;

public static class RelayStateMapper
{
    public static CanaKitRelayState FromAresStruct(AresStruct state)
    {
        return new CanaKitRelayState
        {
            Relay1On = state.Fields.GetValueOrDefault("Relay1On")?.BoolValue ?? false,
            Relay2On = state.Fields.GetValueOrDefault("Relay2On")?.BoolValue ?? false,
            Bypass = state.Fields.GetValueOrDefault("Bypass")?.BoolValue ?? true
        };
    }
}
