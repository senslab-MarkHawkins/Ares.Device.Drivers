using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace TC0304Remastered.Commands;

internal class ToggleTemperatureUnitCommand : SerialCommand
{
  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes("C");
}
