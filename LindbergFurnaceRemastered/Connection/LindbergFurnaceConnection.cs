using Ares.Toolkit.Serial;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace LindbergFurnaceRemastered.Connection;

public class LindbergFurnaceConnection : AresHardwareConnection, ILindbergFurnaceConnection
{
  private readonly List<int> _unusedAddresses;

  public LindbergFurnaceConnection(string portName) : base(new SerialPortConnectionInfo(9600, Parity.Even, 7, StopBits.One), portName,
    new SerialConnectionOptions
    {
      SendBuffer = TimeSpan.FromMilliseconds(50),
      SendTimeout = TimeSpan.FromSeconds(2)
    }
  )
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
}
