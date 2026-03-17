using Ares.Toolkit.Serial;
using Ares.Toolkit.Serial.Simulation;
using LindbergFurnaceRemastered.Commands;
using LindbergFurnaceRemastered.Connection;
using System.Collections.ObjectModel;
using System.Text;

namespace LindbergFurnaceRemastered.Simulation;

public class SimLindbergFurnaceConnection : AresSerialSimConnection, ILindbergFurnaceConnection
{
  private readonly List<int> _unusedAddresses;

  public SimLindbergFurnaceConnection(string portName) : base(new SerialPortConnectionInfo(
      9600,
      System.IO.Ports.Parity.Even,
      7,
      System.IO.Ports.StopBits.One), portName)
  {
    _unusedAddresses = new List<int>(Enumerable.Range(1, 247).ToArray());
    UnusedAddresses = new ReadOnlyCollection<int>(_unusedAddresses);
  }

  public IEnumerable<int> UnusedAddresses { get; }

  public bool ReserveAddress(int address)
  {
    if (!_unusedAddresses.Contains(address))
      return false;

    _unusedAddresses.Remove(address);
    _unusedAddresses.Sort();
    return true;
  }

  public void ReleaseAddress(int address)
  {
    if (_unusedAddresses.Contains(address))
      return;

    _unusedAddresses.Add(address);
    _unusedAddresses.Sort();
  }

  public override void SendInternally(byte[] bytes)
  {
    var requestStr = Encoding.UTF8.GetString(bytes);
    if (requestStr.Length < 5) return;

    var responseStart = requestStr.Substring(1, 4);
    var randomTemp = (new Random().Next(1000) + 20) % 1000;
    var responseBody = $"{responseStart}{4:x2}{randomTemp:X4}";
    var lrc = $"{TubeFurnaceCommandHelper.Lrc(responseBody.Select(c => (byte)c)):X2}";
    var response = $":{responseBody}{lrc}\r\n";
    var responseBytes = Encoding.UTF8.GetBytes(response);
    AddDataReceived(responseBytes);
  }
}
