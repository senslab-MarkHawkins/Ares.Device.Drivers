using Lambda103Shutter.Commands.Responses;
using Lambda103Shutter.Commands.Responses.Parsers;

namespace Lambda103Shutter.Commands.Requests;

public class StatusRequest : Lambda103CommandRequest<StatusResponse>
{
    public StatusRequest() : base(new StatusParser())
    {
    }

    protected override byte[] Serialize()
    {
        return new byte[] { 204 };
    }
}
