using LaserChillerRemastered.Commands;
using LaserChillerRemastered.Commands.Responses;
using LaserChillerRemastered.Commands.Responses.Parsers;

namespace LaserChillerRemastered.Commands.Requests;

public class GetManifoldTemperatureCommand : ChillerCommandExpectingResponse<GetManifoldTemperatureResponse>
{
  public GetManifoldTemperatureCommand() : base(new ManifoldTemperatureParser())
  {
  }

  protected override byte[] Serialize()
  {
    return [0x2E, 0x49, 0x37, 0x37, 0x0D];
  }
}
