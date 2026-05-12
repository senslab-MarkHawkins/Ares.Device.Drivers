using System.Text;

namespace MKS651DRemastered.Simulation;

public class MKS651DSim
{
    private double _pressure = 10.0;
    private double _valvePosition = 0.0;
    private double _minPressure = 0.1;
    private double _maxPressure = 1000;
    private int _activeSetpoint = 1;
    private readonly SetpointData[] _setpoints = new SetpointData[5];

    public MKS651DSim()
    {
        for (int i = 0; i < 5; i++)
        {
            _setpoints[i] = new SetpointData { Index = i + 1, Pressure = 50.0, Gain = 10.0, Soft = 10.0 };
        }
        StartRandomizing();
    }

    public string ProcessCommand(string command)
    {
        command = command.Trim();
        if (command == "R5") return $"P+{_pressure:000.00}";
        if (command == "R6") return $"AA{_valvePosition:000.00}";
        if (command == "R33") return $"AA{_maxPressure:000.00}";
        if (command == "R55") return $"AA{_minPressure:000.00}";
        if (command == "O") { _valvePosition = 100; return "AA"; }
        if (command == "C") { _valvePosition = 0; return "AA"; }
        
        if (command.StartsWith("EH"))
        {
            if (double.TryParse(command.Substring(2), out var val)) _maxPressure = val;
            return "AA";
        }
        if (command.StartsWith("EL"))
        {
            if (double.TryParse(command.Substring(2), out var val)) _minPressure = val;
            return "AA";
        }

        if (command.StartsWith("D"))
        {
            if (int.TryParse(command.Substring(1), out var idx)) _activeSetpoint = idx;
            return "AA";
        }

        // Getters for setpoints
        if (command.StartsWith("R"))
        {
            var rVal = int.Parse(command.Substring(1));
            if (rVal >= 1 && rVal <= 4) return $"AA{_setpoints[rVal - 1].Pressure:000.00}";
            if (rVal == 10) return $"AA{_setpoints[4].Pressure:000.00}";
            if (rVal >= 46 && rVal <= 50) return $"AA{_setpoints[rVal - 46].Gain:000.00}";
            if (rVal >= 15 && rVal <= 19) return $"AA{_setpoints[rVal - 15].Soft:000.00}";
        }

        // Setters for setpoints
        if (command.StartsWith("S"))
        {
             // S[1-5] val
             var parts = command.Substring(1).Split(' ');
             if (int.TryParse(parts[0], out var idx) && double.TryParse(parts[1], out var val))
                 _setpoints[idx-1].Pressure = val * _maxPressure / 100.0;
             return "AA";
        }
        if (command.StartsWith("M"))
        {
             var parts = command.Substring(1).Split(' ');
             if (int.TryParse(parts[0], out var idx) && double.TryParse(parts[1], out var val))
                 _setpoints[idx-1].Gain = val;
             return "AA";
        }
        if (command.StartsWith("I"))
        {
             var parts = command.Substring(1).Split(' ');
             if (int.TryParse(parts[0], out var idx) && double.TryParse(parts[1], out var val))
                 _setpoints[idx-1].Soft = val;
             return "AA";
        }

        return "?";
    }

    private void StartRandomizing()
    {
        Task.Run(async () =>
        {
            var random = new Random();
            while (true)
            {
                var target = _setpoints[_activeSetpoint - 1].Pressure;
                _pressure += (target - _pressure) * 0.1 + (random.NextDouble() - 0.5) * 0.5;
                _valvePosition = Math.Clamp(_valvePosition + (random.NextDouble() - 0.5), 0, 100);
                await Task.Delay(500);
            }
        });
    }

    private class SetpointData
    {
        public int Index { get; set; }
        public double Pressure { get; set; }
        public double Gain { get; set; }
        public double Soft { get; set; }
    }
}
