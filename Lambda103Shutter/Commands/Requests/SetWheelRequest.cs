using Lambda103Shutter.Commands.Responses;
using Lambda103Shutter.Commands.Responses.Parsers;

namespace Lambda103Shutter.Commands.Requests;

public class SetWheelRequest : Lambda103CommandRequest<AckResponse>
{
    private readonly int _position;

    public SetWheelRequest(int position) : base(new AckParser())
    {
        if (position < 0 || position > 9)
            throw new ArgumentOutOfRangeException(nameof(position), "Wheel position must be between 0 and 9.");
        _position = position;
    }

    protected override byte[] Serialize()
    {
        return new byte[] { (byte)(112 + _position) };
    }
}
