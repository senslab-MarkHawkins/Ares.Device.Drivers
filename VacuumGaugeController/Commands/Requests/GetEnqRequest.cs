using Ares.Toolkit.Serial.Commands;
using VacuumGaugeController.Commands.Responses;

namespace VacuumGaugeController.Commands.Requests;

public class GetEnqRequest<T> : SerialCommandWithResponse<T> where T : VacuumGaugeResponse
{
    public GetEnqRequest(SerialResponseParser<T> parser) : base(parser)
    {
    }

    protected override byte[] Serialize()
    {
        return new byte[] { 0x05 };
    }
}
