namespace VacuumGaugeController.UI;

public class VacuumGaugeState
{
    public float Pressure { get; set; }
    public string Status { get; set; } = "Unknown";
    public string ErrorStatus { get; set; } = "None";
}
