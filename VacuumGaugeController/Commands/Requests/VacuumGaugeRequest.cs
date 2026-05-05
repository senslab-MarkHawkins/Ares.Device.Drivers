using Ares.Toolkit.Serial.Commands;
using System.Text;
using VacuumGaugeController.Commands.Responses;

namespace VacuumGaugeController.Commands.Requests;

public abstract class VacuumGaugeRequest<T> : SerialCommandWithResponse<T> where T : VacuumGaugeResponse
{
    protected VacuumGaugeRequest(SerialResponseParser<T> parser) : base(parser)
    {
    }

    protected abstract string CommandText { get; }

    protected override byte[] Serialize()
    {
        // Legacy code: byte[] { (byte)'P', (byte)'R', (byte)'1', 0x0D, 0x0A }
        // then 0x05 (ENQ)
        // The protocol seems to be: Send command\r\n, wait for ACK (0x06), then send ENQ (0x05) to get data.
        
        // However, SerialCommand doesn't easily support this multi-step interaction in a single Serialize call.
        // We'll send the command first.
        
        return Encoding.ASCII.GetBytes($"{CommandText}\r\n");
    }
}
