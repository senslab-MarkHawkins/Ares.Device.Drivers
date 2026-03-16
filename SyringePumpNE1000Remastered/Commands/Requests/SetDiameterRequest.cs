using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class SetDiameterRequest : RequestExpectingResponse<Response>
{
  public SetDiameterRequest(int address, double diameterMm) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    DiameterMm = diameterMm;
  }

  public int Address { get; }
  public double DiameterMm { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Dia.ToProtocolString()} {FormatHelper.FormatToFloatString(DiameterMm)}";
}
