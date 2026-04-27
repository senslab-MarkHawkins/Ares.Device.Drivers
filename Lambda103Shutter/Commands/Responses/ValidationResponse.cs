namespace Lambda103Shutter.Commands.Responses;

public class ValidationResponse : Lambda103Response
{
    public ValidationResponse(byte[] data)
    {
        Data = data;
    }

    public byte[] Data { get; }
    public bool IsValid => Data.Length >= 29;
}
