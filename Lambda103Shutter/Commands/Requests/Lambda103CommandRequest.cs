using Ares.Toolkit.Serial.Commands;
using Lambda103Shutter.Commands.Responses;

namespace Lambda103Shutter.Commands.Requests;

public abstract class Lambda103CommandRequest<T> : SerialCommandWithResponse<T> where T : Lambda103Response
{
    protected Lambda103CommandRequest(SerialResponseParser<T> parser) : base(parser)
    {
    }
}
