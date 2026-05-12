namespace WaterPIDRemastered.UI.State;

public class WaterPIDState
{
    public double TargetPPM { get; set; }
    public double CalculatedOutput { get; set; }
    public string Status { get; set; } = "INACTIVE";
    public double ProportionalContribution { get; set; }
    public double IntegralContribution { get; set; }
    public double DerivativeContribution { get; set; }
}
