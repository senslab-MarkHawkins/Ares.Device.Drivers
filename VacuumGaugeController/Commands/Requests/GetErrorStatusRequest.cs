using VacuumGaugeController.Commands.Responses;
using VacuumGaugeController.Commands.Responses.Parsers;

namespace VacuumGaugeController.Commands.Requests;

public class GetErrorStatusRequest : VacuumGaugeRequest<AckResponse>
{
    public GetErrorStatusRequest() : base(new AckParser())
    {
    }

    protected override string CommandText => "ERR";
}
