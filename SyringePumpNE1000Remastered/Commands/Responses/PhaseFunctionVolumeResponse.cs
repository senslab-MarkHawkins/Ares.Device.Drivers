namespace SyringePumpNE1000Remastered.Commands.Responses;

public sealed class PhaseFunctionVolumeResponse : Response
{
  public PhaseFunctionVolumeResponse(int address, StatusPrompt status, CommandError? error = null) : base(address, status, error)
  {
  }

  public PhaseFunctionVolumeResponse(int address, StatusPrompt status, double volume, VolumeUnit volumeUnit) : base(address, status)
  {
    Volume = volume;
    VolumeUnit = volumeUnit;
  }

  public double Volume { get; }
  public VolumeUnit VolumeUnit { get; } = VolumeUnit.UndefinedVolumeUnit;
}
