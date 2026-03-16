namespace SyringePumpNE1000Remastered;

public enum StatusPrompt
{
  UndefinedStatusPrompt = 0,
  PromptI = 1,
  PromptW = 2,
  PromptS = 3,
  PromptP = 4,
  PromptT = 5,
  PromptU = 6,
  PromptX = 7
}

public enum CommandError
{
  UndefinedError = 0,
  UnrecognizedCommand = 1,
  Na = 2,
  Oor = 3,
  Com = 4,
  Ign = 5
}

public enum RateUnit
{
  UndefinedRateUnit = 0,
  Um = 1,
  Mm = 2,
  Uh = 3,
  Mh = 4
}

public enum VolumeUnit
{
  UndefinedVolumeUnit = 0,
  Ul = 1,
  Ml = 2
}

public enum Direction
{
  UndefinedDirection = 0,
  Inf = 1,
  Wdr = 2,
  Rev = 3,
  Stk = 4
}

public enum SyringePumpFunction
{
  UndefinedFunction = 0,
  Dia = 20,
  Phn = 21,
  Rat = 22,
  Vol = 23,
  Dir = 24,
  Run = 25,
  Pur = 26,
  Stp = 27,
  Dis = 28,
  Cld = 29,
  Adr = 30,
  Reset = 31,
  Fun = 32,
  Ver = 33
}

internal enum SpecialAsciiCharacter
{
  STX = 0x02,
  ETX = 0x03
}
