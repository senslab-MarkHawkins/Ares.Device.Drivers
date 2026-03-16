namespace SyringePumpNE1000Remastered;

public sealed class SyringePumpPhaseState
{
  public int Number { get; set; }
  public SyringePumpFunction Function { get; set; } = SyringePumpFunction.UndefinedFunction;
  public double Rate { get; set; }
  public RateUnit RateUnit { get; set; } = RateUnit.UndefinedRateUnit;
  public double Volume { get; set; }
  public VolumeUnit VolumeUnit { get; set; } = VolumeUnit.UndefinedVolumeUnit;
  public Direction Direction { get; set; } = Direction.UndefinedDirection;
}

public sealed class SyringePumpState
{
  public string FirmwareVersion { get; set; } = string.Empty;
  public double DispensedVolume { get; set; }
  public double WithdrawnVolume { get; set; }
  public VolumeUnit VolumeUnits { get; set; } = VolumeUnit.UndefinedVolumeUnit;
  public int Address { get; set; }
  public double DiameterMm { get; set; }
  public StatusPrompt Status { get; set; } = StatusPrompt.UndefinedStatusPrompt;
  public SyringePumpPhaseState Phase { get; set; } = new();
}
