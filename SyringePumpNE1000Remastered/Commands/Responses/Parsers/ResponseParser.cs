using Ares.Toolkit.Serial.Commands;
using System.Globalization;
using System.Text;

namespace SyringePumpNE1000Remastered.Commands.Responses.Parsers;

public abstract class ResponseParser<T> : SerialResponseParser<T> where T : Response
{
  protected ResponseParser(int address)
  {
    Address = address;
  }

  public int Address { get; }

  protected static bool TryParseFloat(string value, out double result)
    => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

  public override bool TryParseResponse(byte[] buffer, out T? response, out ArraySegment<byte>? dataToRemove)
  {
    var asciiBufferProxy = Encoding.ASCII.GetString(buffer);
    var useLast = asciiBufferProxy.EndsWith((char)SpecialAsciiCharacter.ETX);
    var availablePackets = asciiBufferProxy
      .Split((char)SpecialAsciiCharacter.ETX, StringSplitOptions.RemoveEmptyEntries)
      .SkipLast(useLast ? 0 : 1)
      .ToArray();

    for(var i = 0; i < availablePackets.Length; i++)
    {
      var fullStringResponse = availablePackets[i];
      var stxIndex = fullStringResponse.IndexOf((char)SpecialAsciiCharacter.STX);
      if(stxIndex < 0)
        continue;

      var considerableStringResponse = fullStringResponse[(stxIndex + 1)..];
      if(considerableStringResponse.Length < 3)
        continue;

      if(!int.TryParse(considerableStringResponse[..2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var address))
        continue;

      if(address != Address)
        continue;

      var statusPromptStr = $"Prompt{considerableStringResponse[2]}";
      if(!Enum.TryParse(statusPromptStr, true, out StatusPrompt status))
        continue;

      var content = considerableStringResponse.Length > 3 ? considerableStringResponse[3..] : string.Empty;
      if(TryParseErrorResponse(address, status, content, out response) || TryParseContentResponse(address, status, content, out response))
      {
        var skippedBytes = availablePackets[..i].Sum(s => s.Length + 1);
        var processedSize = availablePackets[i].Length + 1;
        dataToRemove = new ArraySegment<byte>(buffer, skippedBytes, processedSize);
        return true;
      }
    }

    response = null;
    dataToRemove = null;
    return false;
  }

  protected abstract bool TryParseContentResponse(int address, StatusPrompt status, string content, out T? response);

  private static bool TryParseErrorResponse(int address, StatusPrompt status, string content, out T? response)
  {
    response = null;
    if(!content.StartsWith('?'))
      return false;

    var error = content.Length == 1
      ? CommandError.UnrecognizedCommand
      : Enum.TryParse<CommandError>(content[1..], true, out var parsedError)
        ? parsedError
        : CommandError.UndefinedError;

    response = Activator.CreateInstance(typeof(T), address, status, error) as T;
    return response is not null;
  }
}
