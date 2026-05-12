using DynamicData.Binding;
using System.ComponentModel;
using System.Linq;

namespace PillarTempRemastered.PID;

public class TimedQueue<T> : ObservableCollectionExtended<TimedEntry<T>>
{
    private int _maxEntryCount;
    private double _timeSincePreviousEntry;

    public void Enqueue(TimedEntry<T> entry)
    {
        double timeElapsedSincePrevious;
        if (!this.Any())
        {
            timeElapsedSincePrevious = 0;
        }
        else
        {
            var previousEntry = Peek();
            if (previousEntry != null)
            {
                timeElapsedSincePrevious = (entry.Time.Ticks - previousEntry.Time.Ticks) / 10000000.0; // In seconds
            }
            else
            {
                timeElapsedSincePrevious = 0;
            }
            timeElapsedSincePrevious = timeElapsedSincePrevious > 0 ? timeElapsedSincePrevious : 1;
        }

        if (Count >= MaxEntryCount && MaxEntryCount > 0)
        {
            // Remove oldest
            Dequeue();
        }

        // Add newest
        Add(entry);
        TimeSincePreviousEntry = timeElapsedSincePrevious;
    }

    public TimedEntry<T>? Dequeue()
    {
        if (!this.Any()) return null;
        var oldestElement = this.FirstOrDefault();
        if (oldestElement != null) Remove(oldestElement);
        return oldestElement;
    }

    public TimedEntry<T>? PopFront()
    {
        return Dequeue();
    }

    public TimedEntry<T>? Peek()
    {
        return this.LastOrDefault();
    }

    public int MaxEntryCount
    {
        get => _maxEntryCount;
        set
        {
            _maxEntryCount = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxEntryCount)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        }
    }

    public double TimeSincePreviousEntry
    {
        get => _timeSincePreviousEntry;
        set
        {
            _timeSincePreviousEntry = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(TimeSincePreviousEntry)));
        }
    }
}
