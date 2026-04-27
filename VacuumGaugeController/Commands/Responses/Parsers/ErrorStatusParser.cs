using VacuumGaugeController.Enums;
using VacuumGaugeController.Commands.Responses;
using Ares.Toolkit.Serial.Commands;

namespace VacuumGaugeController.Commands.Responses.Parsers;

public class ErrorStatusParser : VacuumGaugeParser<ErrorStatusResponse>
{
    protected override bool TryParseResponse(string line, out ErrorStatusResponse? response)
    {
        response = null;

        // Data format: 4 digit error code
        // Legacy code: errorIndex = errorResponse.Substring(0, 4);
        
        if (line.Length >= 4)
        {
            try
            {
                var errorIndex = line.Substring(0, 4);
                if (int.TryParse(errorIndex, out var errorCode))
                {
                    response = new ErrorStatusResponse((VacuumGaugeControllerErrorStatus)errorCode);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        return false;
    }
}
