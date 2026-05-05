namespace CanaKitRelay.Simulation;

public class RelaySim : IDisposable
{
    private readonly Action<byte[]> _byteSender;
    private bool _relay1On;
    private bool _relay2On;

    public RelaySim(Action<byte[]> byteSender)
    {
        _byteSender = byteSender;
    }

    public void ProcessCommand(byte[] bytes)
    {
        var command = System.Text.Encoding.ASCII.GetString(bytes).Trim();

        if (string.IsNullOrEmpty(command))
        {
            Send("::");
            return;
        }

        if (command == "REL1.ON") _relay1On = true;
        else if (command == "REL1.OFF") _relay1On = false;
        else if (command == "REL2.ON") _relay2On = true;
        else if (command == "REL2.OFF") _relay2On = false;
        else if (command == "REL1.GET") Send($"REL1={(_relay1On ? "1" : "0")}");
        else if (command == "REL2.GET") Send($"REL2={(_relay2On ? "1" : "0")}");
    }

    private void Send(string response)
    {
        _byteSender(System.Text.Encoding.ASCII.GetBytes(response + "\r\n"));
    }

    public void Dispose()
    {
    }
}
