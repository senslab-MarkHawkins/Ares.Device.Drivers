using Ares.Datamodel;

namespace VacuumPumpPlugin.UI.State;

public static class VacuumStateMapper
{
  public static VacuumState FromAresStruct(AresStruct state)
  {
    return new VacuumState
    {
      PumpStatus = state.Fields.GetValueOrDefault("PumpStatus")?.StringValue ?? "Unknown",
      RotationSpeed = (int)(state.Fields.GetValueOrDefault("RotationSpeed")?.NumberValue ?? 0)
    };
  }
}
