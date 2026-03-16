using System.Text;

namespace TC0304Remastered.Simulation;

internal sealed class SimulatedDataLogger : IDisposable
{
  private readonly Action<byte[]> _byteSender;
  private readonly CancellationTokenSource _cts = new();
  private readonly Task _tempUpdater;
  private bool _celsius = true;
  private bool _hold;
  private int _probe1Temperature = 300;
  private int _probe2Temperature = 300;

  public SimulatedDataLogger(Action<byte[]> byteSender)
  {
    _byteSender = byteSender;
    _tempUpdater = StartTempUpdater(_cts.Token);
  }

  public void Dispose()
  {
    _cts.Cancel();
    try
    {
      _tempUpdater.Wait();
    }
    catch(AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
    {
    }
    _cts.Dispose();
  }

  public void SendCommand(byte[] command)
  {
    var random = new Random();
    Task.Delay(random.Next(100, 300), _cts.Token).ContinueWith(_ =>
    {
      if(_cts.IsCancellationRequested)
        return;

      var cmd = Encoding.ASCII.GetString(command);
      ProcessCommand(cmd);
    }, CancellationToken.None);
  }

  private Task StartTempUpdater(CancellationToken token)
  {
    return Task.Factory.StartNew(() =>
    {
      var random = new Random();
      Thread.CurrentThread.Name = "Sim Datalogger Temperature Randomizer Thread";
      while(!token.IsCancellationRequested)
      {
        if(!_hold)
        {
          _probe1Temperature = random.Next(100, 900);
          _probe2Temperature = random.Next(100, 900);
        }

        Task.Delay(500, token).Wait(token);
      }
    }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
  }

  private void ProcessCommand(string command)
  {
    if(command.StartsWith('A'))
      SendData();
    else if(command.StartsWith('H'))
      _hold = !_hold;
    else if(command.StartsWith('C'))
      _celsius = !_celsius;
  }

  private void SendData()
  {
    var data = new byte[45];
    Array.Fill(data, (byte)0);
    data[0] = 2;
    data[44] = 3;

    if(_hold)
      data[1] += 0b_0010_0000;

    if(_celsius)
      data[1] += 0b_1000_0000;

    data[7] = (byte)(_probe1Temperature >> 8);
    data[8] = (byte)_probe1Temperature;
    data[9] = (byte)(_probe2Temperature >> 8);
    data[10] = (byte)_probe2Temperature;
    data[11] = 0x7F;
    data[12] = 0xFF;
    data[13] = 0x7F;
    data[14] = 0xFF;

    _byteSender(data);
  }
}
