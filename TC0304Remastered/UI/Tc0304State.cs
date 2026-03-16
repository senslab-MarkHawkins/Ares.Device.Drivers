namespace TC0304Remastered.UI;

public sealed class Tc0304State
{
  public bool BatteryLow { get; init; }
  public bool Hold { get; init; }
  public bool Celsius { get; init; }
  public string Mode { get; init; } = "Normal";
  public string Probe1Name { get; init; } = "Probe 1";
  public string Probe2Name { get; init; } = "Probe 2";
  public string Probe3Name { get; init; } = "Probe 3";
  public string Probe4Name { get; init; } = "Probe 4";
  public double? T1Probe { get; init; }
  public double? T2Probe { get; init; }
  public double? T3Probe { get; init; }
  public double? T4Probe { get; init; }
}
