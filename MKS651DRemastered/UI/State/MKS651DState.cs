namespace MKS651DRemastered.UI.State;

public class MKS651DState
{
    public double Pressure { get; set; }
    public double ValvePosition { get; set; }
    public int ActiveSetpoint { get; set; }
    public List<SetpointState> Setpoints { get; set; } = new();
}

public class SetpointState
{
    public int Index { get; set; }
    public double Pressure { get; set; }
    public double Gain { get; set; }
    public double Soft { get; set; }
}
