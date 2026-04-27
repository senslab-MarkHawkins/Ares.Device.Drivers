using VacuumGaugeController.Enums;
using VacuumGaugeController.Commands.Responses;
using Ares.Toolkit.Serial.Commands;
using System.Text.RegularExpressions;

namespace VacuumGaugeController.Commands.Responses.Parsers;

public class PressureParser : VacuumGaugeParser<PressureResponse>
{
    protected override bool TryParseResponse(string line, out PressureResponse? response)
    {
        response = null;

        // Data format: status, pressure (sx.xxxxEsxx)
        // Example: 0, 1.2345E-03
        // Legacy code: statusStr = response.Substring(0, 1); pressureStr = response.Substring(2, 11);
        
        if (line.Length >= 13)
        {
            try
            {
                var statusStr = line.Substring(0, 1);
                if (int.TryParse(statusStr, out var statusIndex))
                {
                    var status = (VacuumGaugeControllerPressureStatus)statusIndex;
                    var pressureStr = line.Substring(2, 11);
                    if (float.TryParse(pressureStr, out var pressure))
                    {
                        response = new PressureResponse(pressure, status);
                        return true;
                    }
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
