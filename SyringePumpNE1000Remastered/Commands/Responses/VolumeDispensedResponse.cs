namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class VolumeDispensedResponse : Response
{
  public VolumeDispensedResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public VolumeDispensedResponse(int address, StatusPrompt status, double infused, double withdrawn, VolumeUnit systemVolumeUnit) : base(address, status)
  {
    Infused = infused;
    Withdrawn = withdrawn;
    SystemVolumeUnit = systemVolumeUnit;
  }

  public double Infused { get; }
  public double Withdrawn { get; }
  public VolumeUnit SystemVolumeUnit { get; } = VolumeUnit.UndefinedVolumeUnit;
}
