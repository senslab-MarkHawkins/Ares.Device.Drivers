namespace PillarTempRemastered.UI.State;

public class PillarTempState
{
    public double TargetTemperature { get; set; }
    public double CalculatedOutput { get; set; }
    public string Status { get; set; } = "INACTIVE";
    public double ProportionalContribution { get; set; }
    public double IntegralContribution { get; set; }
    public double DerivativeContribution { get; set; }
}
