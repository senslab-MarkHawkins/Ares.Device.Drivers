namespace SyringePumpNE1000Remastered.Commands;

internal static class Crc16Ccitt
{
  public static ushort Compute(byte[] bytes)
  {
    const ushort polynomial = 0x1021;
    ushort crc = 0;

    foreach(var value in bytes)
    {
      crc ^= (ushort)(value << 8);
      for(var i = 0; i < 8; i++)
      {
        crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ polynomial : crc << 1);
      }
    }

    return crc;
  }
}
