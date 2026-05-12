using ReactiveUI;
using System;

namespace WaterPIDRemastered.PID;

public class TimedEntry<T> : ReactiveObject
{
    private T _data;
    private DateTime _time;

    public TimedEntry(T data)
    {
        _data = data;
        _time = DateTime.UtcNow;
    }

    public T Data
    {
        get => _data;
        set => this.RaiseAndSetIfChanged(ref _data, value);
    }

    public DateTime Time
    {
        get => _time;
        set => this.RaiseAndSetIfChanged(ref _time, value);
    }
}
