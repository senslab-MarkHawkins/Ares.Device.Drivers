using Ares.Toolkit.Serial.Commands;

namespace CanaKitRelay.Commands.Responses;

public abstract class CanaKitRelayResponse : SerialResponse
{
}

public class RelayStateResponse : CanaKitRelayResponse
{
    public RelayStateResponse(int relayNumber, bool isOn)
    {
        RelayNumber = relayNumber;
        IsOn = isOn;
    }

    public int RelayNumber { get; }
    public bool IsOn { get; }
}

public class GenericRelayResponse : CanaKitRelayResponse
{
}
