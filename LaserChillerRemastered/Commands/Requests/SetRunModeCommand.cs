using Ares.Toolkit.Serial.Commands;

namespace LaserChillerRemastered.Commands.Requests;

public class SetRunModeCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return [0x2E, 0x47, 0x31, 0x41, 0x36, 0x0D];
  }
}
