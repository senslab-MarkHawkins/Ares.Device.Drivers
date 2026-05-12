using System.Linq;
using ReactiveUI;
using System;

namespace PillarTempRemastered.PID;

public class SignalMeasurePIDController : ReactiveObject
{
    private readonly SignalMeasurePIDDataContainer _dataContainer;
    private double _previousIntegral;
    private double _previousError;
    private double _integral;
    private double _measureError;
    private double _derivative;

    private bool _integralActive = true;
    private double _proportionalContribution;
    private double _integralContribution;
    private double _derivativeContribution;
    private double _baselineSignal;

    public double Kp { get; set; }
    public double Ki { get; set; }
    public double Kd { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double ChangeLimit { get; set; }
    public double IntegralPrecision { get; set; } = 1.0;
    public int SignalRoundingDecimalPlaces { get; set; } = 2;

    public SignalMeasurePIDController(int signalsToAvg = 5, int measuresToAvg = 1)
    {
        _dataContainer = new SignalMeasurePIDDataContainer(signalsToAvg, measuresToAvg);
        _baselineSignal = 0;
    }

    public double CalculateCommandValue(double target, double measured)
    {
        var previousSignal = _dataContainer.SignalContainer.Peek()?.Data ?? 0;
        
        _dataContainer.SignalContainer.Enqueue(new TimedEntry<double>(0));
        _dataContainer.MeasureContainer.Enqueue(new TimedEntry<double>(measured));
        
        double signalDt = _dataContainer.SignalContainer.TimeSincePreviousEntry;
        if (Math.Abs(signalDt) < 0.000001) signalDt = 1;

        double measureDt = _dataContainer.MeasureContainer.TimeSincePreviousEntry;
        if (Math.Abs(measureDt) < 0.000001) measureDt = 1;

        if (_dataContainer.MeasureContainer.Any())
            MeasureError = target - _dataContainer.MeasureContainer.Average(entry => entry.Data);
        else
            MeasureError = target - measured;

        Integral = PreviousIntegral;
        var newIntegral = PreviousIntegral + MeasureError * signalDt;
        var potentialNewIntegralContribution = Ki * newIntegral;
        var previousIntegralContribution = Ki * PreviousIntegral;
        
        if (Math.Abs(previousIntegralContribution - potentialNewIntegralContribution) >= ChangeLimit / IntegralPrecision)
            Integral = newIntegral;

        Derivative = (MeasureError - PreviousError) / measureDt;
        PreviousError = MeasureError;
        PreviousIntegral = Integral;

        ProportionalContribution = Kp * MeasureError;
        IntegralContribution = _integralActive ? Ki * Integral : 0;
        DerivativeContribution = Kd * Derivative;

        var signal = ProportionalContribution + IntegralContribution + DerivativeContribution + BaselineSignal;
        var clampedSignal = ClampSignal(signal);

        // Minimize integral windup
        if (Math.Abs(clampedSignal - signal) > 0.000001 && signal < Min && SignsEqual(signal, MeasureError))
        {
            _integralActive = false;
            PreviousIntegral = 0;
        }
        else
        {
            _integralActive = true;
        }

        var currentPeek = _dataContainer.SignalContainer.Peek();
        if (currentPeek != null)
        {
            currentPeek.Data = clampedSignal;
            var avgSignal = _dataContainer.SignalContainer.Average(entry => entry.Data);
            avgSignal = Math.Round(avgSignal, SignalRoundingDecimalPlaces);
            currentPeek.Data = avgSignal;
            return Math.Abs(previousSignal - avgSignal) >= ChangeLimit ? avgSignal : previousSignal;
        }

        return clampedSignal;
    }

    public void Reset()
    {
        MeasureError = 0;
        PreviousError = 0;
        Derivative = 0;
        PreviousIntegral = 0;
        Integral = 0;
        BaselineSignal = Min;
        _integralActive = true;
        _dataContainer.MeasureContainer.Clear();
        _dataContainer.SignalContainer.Clear();
    }

    public double ProportionalContribution
    {
        get => _proportionalContribution;
        private set => this.RaiseAndSetIfChanged(ref _proportionalContribution, value);
    }

    public double IntegralContribution
    {
        get => _integralContribution;
        private set => this.RaiseAndSetIfChanged(ref _integralContribution, value);
    }

    public double DerivativeContribution
    {
        get => _derivativeContribution;
        private set => this.RaiseAndSetIfChanged(ref _derivativeContribution, value);
    }

    public double BaselineSignal
    {
        get => _baselineSignal;
        set => this.RaiseAndSetIfChanged(ref _baselineSignal, value);
    }

    public double ClampSignal(double signal)
    {
        if (double.IsNaN(signal) || double.IsInfinity(signal)) return Min;
        if (signal > Max) return Max;
        if (signal < Min) return Min;
        return signal;
    }

    private static bool SignsEqual(double num1, double num2)
    {
        return (num1 >= 0 && num2 >= 0) || (num1 < 0 && num2 < 0);
    }

    private double PreviousIntegral
    {
        get => _previousIntegral;
        set => this.RaiseAndSetIfChanged(ref _previousIntegral, value);
    }

    private double PreviousError
    {
        get => _previousError;
        set => this.RaiseAndSetIfChanged(ref _previousError, value);
    }

    private double Integral
    {
        get => _integral;
        set => this.RaiseAndSetIfChanged(ref _integral, value);
    }

    private double MeasureError
    {
        get => _measureError;
        set => this.RaiseAndSetIfChanged(ref _measureError, value);
    }

    private double Derivative
    {
        get => _derivative;
        set => this.RaiseAndSetIfChanged(ref _derivative, value);
    }
}
