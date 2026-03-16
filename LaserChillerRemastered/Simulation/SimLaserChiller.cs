using System.Text;

namespace LaserChillerRemastered.Simulation;

public class SimLaserChiller
{
  private readonly Action<byte[]> _byteSender;
  private static readonly byte[] SetStandbyCommandData = [0x2E, 0x47, 0x30, 0x41, 0x35, 0x0D];
  private static readonly byte[] SetRunModeCommandData = [0x2E, 0x47, 0x31, 0x41, 0x36, 0x0D];
  private static readonly byte[] ReadManifoldTempCommandData = [0x2E, 0x49, 0x37, 0x37, 0x0D];

  public SimLaserChiller(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
  }

  public SimChillerModeEnum Mode { get; set; } = SimChillerModeEnum.Standby;
  public double ManifoldTemperature { get; set; } = 20.0d;

  public void SendCommand(byte[] data)
  {
    if(data.SequenceEqual(SetStandbyCommandData))
    {
      Mode = SimChillerModeEnum.Standby;
      return;
    }

    if(data.SequenceEqual(SetRunModeCommandData))
    {
      Mode = SimChillerModeEnum.Running;
      return;
    }

    if(data.SequenceEqual(ReadManifoldTempCommandData))
    {
      var sign = ManifoldTemperature < 0 ? '-' : '+';
      var scaledTemperature = (int)Math.Round(Math.Abs(ManifoldTemperature) * 10, MidpointRounding.AwayFromZero);
      var payload = $"#I0{sign}{scaledTemperature:0000}\r";
      _byteSender(Encoding.ASCII.GetBytes(payload));
    }
  }
}
