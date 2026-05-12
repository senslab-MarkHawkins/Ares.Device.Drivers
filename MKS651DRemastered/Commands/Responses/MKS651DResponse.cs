using Ares.Toolkit.Serial.Commands;

namespace MKS651DRemastered.Commands.Responses;

public class MKS651DResponse : SerialResponse
{
    public MKS651DResponse(string rawResponse)
    {
        RawResponse = rawResponse;
    }

    public string RawResponse { get; }
}

public class MKS651DNumericResponse : MKS651DResponse
{
    public MKS651DNumericResponse(string rawResponse, double value) : base(rawResponse)
    {
        Value = value;
    }

    public double Value { get; }
}
