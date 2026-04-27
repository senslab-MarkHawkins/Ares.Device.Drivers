using Lambda103Shutter.Commands.Responses;
using Lambda103Shutter.Commands.Responses.Parsers;

namespace Lambda103Shutter.Commands.Requests;

public class ValidationRequest : Lambda103CommandRequest<ValidationResponse>
{
    public ValidationRequest() : base(new ValidationParser())
    {
    }

    protected override byte[] Serialize()
    {
        return new byte[] { 238, 253 };
    }
}
