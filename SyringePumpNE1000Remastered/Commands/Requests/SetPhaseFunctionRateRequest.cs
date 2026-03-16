using SyringePumpNE1000Remastered.Commands.Responses;
using SyringePumpNE1000Remastered.Commands.Responses.Parsers;

namespace SyringePumpNE1000Remastered.Commands.Requests;

internal sealed class SetPhaseFunctionRateRequest : RequestExpectingResponse<Response>
{
  public SetPhaseFunctionRateRequest(int address, double rate, RateUnit rateUnit) : base(new ConfirmationResponseParser(address))
  {
    Address = address;
    Rate = rate;
    RateUnit = rateUnit;
  }

  public int Address { get; }
  public double Rate { get; }
  public RateUnit RateUnit { get; }

  protected override string GenerateCommandString()
    => $"{Address} {SyringePumpFunction.Rat.ToProtocolString()} C {FormatHelper.FormatToFloatString(Rate)} {RateUnit.ToProtocolString()}";
}
