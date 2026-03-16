using Ares.Toolkit.Serial.Commands;
using SyringePumpNE1000Remastered.Commands.Responses;
using System.Text;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal abstract class RequestExpectingResponse<TResponse> : SerialCommandWithResponse<TResponse> where TResponse : Response
{
  protected RequestExpectingResponse(SerialResponseParser<TResponse> parser, bool safeMode = false) : base(parser)
  {
    SafeMode = safeMode;
  }

  protected bool SafeMode { get; }

  protected sealed override byte[] Serialize()
  {
    var commandString = GenerateCommandString();
    return SafeMode ? GenerateSafeOutboundCommand(commandString) : GenerateBasicOutboundCommand(commandString);
  }

  protected abstract string GenerateCommandString();

  private static byte[] GenerateBasicOutboundCommand(string commandData)
    => Encoding.ASCII.GetBytes($"{commandData}\r");

  private static byte[] GenerateSafeOutboundCommand(string commandData)
  {
    var commandBytes = Encoding.ASCII.GetBytes(commandData);
    var packetLength = 1 + commandBytes.Length + 2;
    var crc16 = Crc16Ccitt.Compute(commandBytes);

    return
    [
      (byte)SpecialAsciiCharacter.STX,
      (byte)packetLength,
      .. commandBytes,
      (byte)(crc16 >> 8),
      (byte)crc16,
      (byte)SpecialAsciiCharacter.ETX
    ];
  }
}
