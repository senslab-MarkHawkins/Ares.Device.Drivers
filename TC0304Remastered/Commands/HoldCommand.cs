using Ares.Toolkit.Serial.Commands;
using System.Text;

namespace TC0304Remastered.Commands;

internal class HoldCommand : SerialCommand
{
  protected override byte[] Serialize()
    => Encoding.ASCII.GetBytes("H");
}
