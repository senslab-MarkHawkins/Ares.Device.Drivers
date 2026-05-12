namespace WaterPIDRemastered.PID;

public class PIDDataContainer : TimedQueue<double>
{
    public PIDDataContainer(int maxSize)
    {
        MaxEntryCount = maxSize;
    }
}
