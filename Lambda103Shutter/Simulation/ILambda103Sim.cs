namespace Lambda103Shutter.Simulation;

public interface ILambda103Sim : IDisposable
{
    Task SendCommand(byte[] command);
}
