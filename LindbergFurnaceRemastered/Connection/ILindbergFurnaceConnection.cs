using Ares.Toolkit.Serial;

namespace LindbergFurnaceRemastered.Connection;

public interface ILindbergFurnaceConnection : IAresSerialConnection
{
  IEnumerable<int> UnusedAddresses { get; }

  bool ReserveAddress(int address);

  void ReleaseAddress(int address);
}
