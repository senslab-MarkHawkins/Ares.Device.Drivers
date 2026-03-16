using Ares.Toolkit.Serial.Commands;

namespace LaserChillerRemastered.Commands.Requests;

public class SetStandbyModeCommand : SerialCommand
{
  protected override byte[] Serialize()
  {
    return [0x2E, 0x47, 0x30, 0x41, 0x35, 0x0D];
  }
}
