using Ares.Toolkit.Serial.Commands;
using ShawHygrometer.Commands.Responses;
using ShawHygrometer.Commands.Responses.Parsers;

namespace ShawHygrometer.Commands.Requests;

public class GetWaterPpmRequest : SerialCommandWithResponse<WaterPpmResponse>
{
    public GetWaterPpmRequest() : base(new WaterPpmParser())
    {
    }

    protected override byte[] Serialize()
    {
        // 0xff, 0xff, 0xff, 0xff, 0xff, 0x02, 0x00, 24, 0x01, 0x00, 27
        return new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0x02, 0x00, 24, 0x01, 0x00, 27 };
    }
}
