using Ares.Datamodel;
using LindbergFurnaceRemastered.UI.State;

namespace LindbergFurnaceRemastered.UI.State;

public static class LindbergFurnaceStateMapper
{
  public static LindbergFurnaceState FromAresStruct(AresStruct aresStruct)
  {
    return new LindbergFurnaceState
    {
      Id = aresStruct.Fields.TryGetValue("Id", out var id) ? id.StringValue : string.Empty,
      Name = aresStruct.Fields.TryGetValue("Name", out var name) ? name.StringValue : string.Empty,
      CurrentTemperature = aresStruct.Fields.TryGetValue("Current Temperature", out var ct) ? ct.NumberValue : 0,
      Setpoint = aresStruct.Fields.TryGetValue("Setpoint", out var sp) ? sp.NumberValue : 0,
      Address = aresStruct.Fields.TryGetValue("Address", out var addr) ? (int)addr.NumberValue : 0
    };
  }
}
