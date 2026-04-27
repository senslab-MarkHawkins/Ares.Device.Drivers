using System.Text;
using VacuumPumpPlugin.Enums;

namespace VacuumPumpPlugin.Simulation;

public class VacuumSim : IVacuumSim
{
  private readonly Action<byte[]> _sendToBuffer;
  private readonly Random _random = new();
  private VacuumPumpStatus _status = VacuumPumpStatus.Normal;
  private int _speed = 5000;

  public VacuumSim(string portName, Action<byte[]> sendToBuffer)
  {
    PortName = portName;
    _sendToBuffer = sendToBuffer;
  }

  public string PortName { get; }

  public void ProcessCommand(byte[] command)
  {
    if (command.Length < 5) return;

    // Check for GetPumpStatus (205)
    if (command[2] == '2' && command[3] == '0' && command[4] == '5')
    {
      // Response: 0x02 0x80 '205' 0x30 STATUS 0x03 ...
      // In legacy code, it expects status at index 5. 
      // Let's match the legacy parser expectation: response[5] is the status char.
      var response = new byte[10];
      response[0] = 0x02;
      response[1] = 0x80;
      response[2] = (byte)'2';
      response[3] = (byte)'0';
      response[4] = (byte)'5';
      response[5] = (byte)((int)_status).ToString()[0];
      response[6] = 0x03;
      _sendToBuffer(response);
    }
    // Check for GetRotationSpeed (210)
    else if (command[2] == '2' && command[3] == '1' && command[4] == '0')
    {
      // Legacy code expects first 4 chars to be the speed.
      _speed = Math.Clamp(_speed + _random.Next(-50, 50), 0, 10000);
      var speedStr = _speed.ToString("D4");
      var response = Encoding.ASCII.GetBytes(speedStr);
      _sendToBuffer(response);
    }
    // Check for Test (010)
    else if (command[2] == '0' && command[3] == '1' && command[4] == '0')
    {
      var response = Encoding.ASCII.GetBytes("OK");
      _sendToBuffer(response);
    }
  }

  public void Dispose()
  {
  }
}
