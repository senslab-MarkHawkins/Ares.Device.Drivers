namespace Lambda103Shutter.Commands.Responses;

public class StatusResponse : Lambda103Response
{
    public StatusResponse(int filterWheel, bool shutterOpen)
    {
        FilterWheel = filterWheel;
        ShutterOpen = shutterOpen;
    }

    public int FilterWheel { get; }
    public bool ShutterOpen { get; }
}
