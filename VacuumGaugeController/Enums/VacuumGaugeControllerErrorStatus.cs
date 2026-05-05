namespace VacuumGaugeController.Enums;

public enum VacuumGaugeControllerErrorStatus
{
    NoError = 0,
    ControllerError = 1000,
    NoHardware = 100,
    ParameterError = 10,
    SyntaxError = 1
}
