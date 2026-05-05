using VacuumGaugeController.Commands.Responses;
using VacuumGaugeController.Commands.Responses.Parsers;

namespace VacuumGaugeController.Commands.Requests;

public class GetPressureRequest : VacuumGaugeRequest<AckResponse>
{
    public GetPressureRequest() : base(new AckParser())
    {
    }

    protected override string CommandText => "PR1";
}
