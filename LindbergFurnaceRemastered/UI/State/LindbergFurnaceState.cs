namespace LindbergFurnaceRemastered.UI.State;

public class LindbergFurnaceState
{
  public string Id { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public double CurrentTemperature { get; set; }
  public double Setpoint { get; set; }
  public int Address { get; set; }
}
