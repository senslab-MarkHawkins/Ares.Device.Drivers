namespace SyringePumpNE1000Remastered;

public enum SyringePumpNE1000Command
{
  QueryPhaseFunction,
  SetPhase,
  SetPhaseFunction,
  QueryPhase,
  SetDiameter,
  GetDiameter,
  SetProgramFunctionRate,
  GetProgramFunctionRate,
  SetProgramFunctionVolumeToBeDispensed,
  GetProgramFunctionVolumeToBeDispensed,
  SetProgramFunctionPumpingDirection,
  GetProgramFunctionPumpingDirection,
  StartPumpingProgram,
  PurgePump,
  StopPumpingProgram,
  GetVolumeDispensed,
  ClearVolumeDispensed
}
