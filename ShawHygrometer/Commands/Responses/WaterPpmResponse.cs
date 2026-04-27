namespace ShawHygrometer.Commands.Responses;

public class WaterPpmResponse : WaterResponse
{
    public WaterPpmResponse(float waterPpm)
    {
        WaterPpm = waterPpm;
    }

    public float WaterPpm { get; }
}
