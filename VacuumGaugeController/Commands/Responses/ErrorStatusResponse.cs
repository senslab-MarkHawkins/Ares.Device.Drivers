using VacuumGaugeController.Enums;

namespace VacuumGaugeController.Commands.Responses;

public class ErrorStatusResponse : VacuumGaugeResponse
{
    public ErrorStatusResponse(VacuumGaugeControllerErrorStatus status)
    {
        Status = status;
    }

    public VacuumGaugeControllerErrorStatus Status { get; }
}
