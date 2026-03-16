using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class SetPhaseFunctionVolumeRequest : RequestExpectingResponse<Response>
{
  public SetPhaseFunctionVolumeRequest(int address, double volumeMl) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    VolumeMl = volumeMl;
  }

  public int Address { get; }
  public double VolumeMl { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Vol.ToProtocolString()} {FormatHelper.FormatToFloatString(VolumeMl)}";
}
