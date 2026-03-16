using LaserChillerRemastered.Commands;

namespace LaserChillerRemastered.Commands.Responses;

public class GetManifoldTemperatureResponse : CommandResponse
{
  public GetManifoldTemperatureResponse(double temperature)
  {
    Temperature = temperature;
  }

  public double Temperature { get; set; }
}
