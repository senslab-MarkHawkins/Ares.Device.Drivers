using Ares.Toolkit.Serial.Commands;

namespace LaserChillerRemastered.Commands;

public abstract class ChillerCommandExpectingResponse<T> : SerialCommandWithResponse<T> where T : CommandResponse
{
  protected ChillerCommandExpectingResponse(SerialResponseParser<T> parser) : base(parser)
  {
  }
}
