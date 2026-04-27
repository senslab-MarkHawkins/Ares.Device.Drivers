namespace VacuumPumpPlugin.Commands.Responses;

public class RotationSpeedResponse : VacuumResponse
{
  public RotationSpeedResponse(int speed)
  {
    Speed = speed;
  }

  public int Speed { get; }
}
