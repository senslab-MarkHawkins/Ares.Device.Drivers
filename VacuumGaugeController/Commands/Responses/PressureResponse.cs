using VacuumGaugeController.Enums;

namespace VacuumGaugeController.Commands.Responses;

public class PressureResponse : VacuumGaugeResponse
{
    public PressureResponse(float pressure, VacuumGaugeControllerPressureStatus status)
    {
        Pressure = pressure;
        Status = status;
    }

    public float Pressure { get; }
    public VacuumGaugeControllerPressureStatus Status { get; }
}
