using Lambda103Shutter.Commands.Responses;
using Lambda103Shutter.Commands.Responses.Parsers;

namespace Lambda103Shutter.Commands.Requests;

public class SetShutterRequest : Lambda103CommandRequest<AckResponse>
{
    private readonly bool _open;

    public SetShutterRequest(bool open) : base(new AckParser())
    {
        _open = open;
    }

    protected override byte[] Serialize()
    {
        return _open ? new byte[] { 170 } : new byte[] { 172 };
    }
}
