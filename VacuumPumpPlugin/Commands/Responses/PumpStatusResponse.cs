using VacuumPumpPlugin.Enums;

namespace VacuumPumpPlugin.Commands.Responses;

public class PumpStatusResponse : VacuumResponse
{
  public PumpStatusResponse(VacuumPumpStatus status)
  {
    Status = status;
  }

  public VacuumPumpStatus Status { get; }
}
