using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class GetFirmwareVersionRequest : RequestExpectingResponse<FirmwareQueryResponse>
{
  public GetFirmwareVersionRequest(int address) : base(new FirmwareQueryResponseParser(address), false)
  {
    Address = address;
  }

  public int Address { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Ver.ToProtocolString()}";
}
