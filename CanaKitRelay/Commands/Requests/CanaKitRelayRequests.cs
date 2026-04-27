using Ares.Toolkit.Serial.Commands;
using CanaKitRelay.Commands.Responses;
using CanaKitRelay.Commands.Responses.Parsers;
using System.Text;

namespace CanaKitRelay.Commands.Requests;

public abstract class CanaKitRelayRequest<T> : SerialCommandWithResponse<T> where T : CanaKitRelayResponse
{
    protected CanaKitRelayRequest() : base(new RelayResponseParser<T>())
    {
    }

    protected abstract string SerializeToString();

    protected override byte[] Serialize()
    {
        return Encoding.ASCII.GetBytes(SerializeToString() + "\r\n");
    }
}

public class RelayOnRequest : CanaKitRelayRequest<CanaKitRelayResponse>
{
    private readonly int _relayNumber;
    public RelayOnRequest(int relayNumber) => _relayNumber = relayNumber;
    protected override string SerializeToString() => $"REL{_relayNumber}.ON";
}

public class RelayOffRequest : CanaKitRelayRequest<CanaKitRelayResponse>
{
    private readonly int _relayNumber;
    public RelayOffRequest(int relayNumber) => _relayNumber = relayNumber;
    protected override string SerializeToString() => $"REL{_relayNumber}.OFF";
}

public class RelayGetRequest : CanaKitRelayRequest<CanaKitRelayResponse>
{
    private readonly int _relayNumber;
    public RelayGetRequest(int relayNumber) => _relayNumber = relayNumber;
    protected override string SerializeToString() => $"REL{_relayNumber}.GET";
}

public class RelayPingRequest : CanaKitRelayRequest<CanaKitRelayResponse>
{
    protected override string SerializeToString() => "";
}
