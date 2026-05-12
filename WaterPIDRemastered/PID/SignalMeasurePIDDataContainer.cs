using System.Linq;
using ReactiveUI;

namespace WaterPIDRemastered.PID;

public class SignalMeasurePIDDataContainer : ReactiveObject
{
    private int _signalCountToAverage;
    private int _measureCountToAverage;

    private PIDDataContainer _signalContainer;
    private PIDDataContainer _measureContainer;

    public SignalMeasurePIDDataContainer(int signalsToAverage = 4, int measuresToAverage = 10)
    {
        _measureContainer = new PIDDataContainer(measuresToAverage > 0 ? measuresToAverage : 1);
        _signalContainer = new PIDDataContainer(signalsToAverage > 0 ? signalsToAverage : 1);
        _measureCountToAverage = measuresToAverage;
        _signalCountToAverage = signalsToAverage;
    }

    public PIDDataContainer SignalContainer
    {
        get => _signalContainer;
        set => this.RaiseAndSetIfChanged(ref _signalContainer, value);
    }

    public PIDDataContainer MeasureContainer
    {
        get => _measureContainer;
        set => this.RaiseAndSetIfChanged(ref _measureContainer, value);
    }

    public int SignalCountToAverage
    {
        get => _signalCountToAverage;
        set
        {
            this.RaiseAndSetIfChanged(ref _signalCountToAverage, value);
            var newSignalContainer = new PIDDataContainer(value);
            SignalContainer = newSignalContainer;
        }
    }

    public int MeasureCountToAverage
    {
        get => _measureCountToAverage;
        set
        {
            this.RaiseAndSetIfChanged(ref _measureCountToAverage, value);
            var newMeasureContainer = new PIDDataContainer(value);
            MeasureContainer = newMeasureContainer;
        }
    }
}
