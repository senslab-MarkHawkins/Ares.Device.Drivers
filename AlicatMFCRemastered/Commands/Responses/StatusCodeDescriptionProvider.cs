namespace AlicatMFCRemastered.Commands.Responses;

/// <summary>
/// Source: DOC-SERIALPRIMER rev1 -- August 25, 2020
/// </summary>
public static class StatusCodeDescriptionProvider
{
  public static string GetDescription(MfcStatusCode code)
  {
    return code switch
    {
      MfcStatusCode.Adc => "Internal communication error (not common – requires repair at factory)",
      MfcStatusCode.Exh => "Manual exhaust valve override (max drive on downstream valve)",
      MfcStatusCode.Hld => "Valve drive hold is active (bypass active PID control)",
      MfcStatusCode.Lck => "Membrane button lockout is active (see command codes below)",
      MfcStatusCode.Mov => "Mass flow rate overage (outside measurable range including uncalibrated range)",
      MfcStatusCode.Opl => "Over pressure limit has been activated",
      MfcStatusCode.Ovr => "Totalizer has rolled over at least once or frozen at max value",
      MfcStatusCode.Pov => "Pressure reading overage (outside measurable range including uncalibrated\r\nrange)",
      MfcStatusCode.Tov => "Temperature reading overage (outside measurable range)",
      MfcStatusCode.Tmf => "Totalizer missed some flow data (due to MOV or VOV error)",
      MfcStatusCode.Vov => "Volumetric flow rate overage (outside measurable range including uncalibrated\r\nrange",
      MfcStatusCode.Na => "Unknown",
      _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };
  }
}
