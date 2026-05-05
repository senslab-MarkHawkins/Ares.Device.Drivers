using Ares.Toolkit.Serial.Commands;
using VacuumGaugeController.Commands.Responses;

namespace VacuumGaugeController.Commands.Responses.Parsers;

public abstract class VacuumGaugeParser<T> : AsciiResponseParser<T> where T : VacuumGaugeResponse
{
}
