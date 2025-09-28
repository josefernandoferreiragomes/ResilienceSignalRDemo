namespace ProducerApi;

public class ErrorChanceStore
{
    private double _errorChance = 0.3;
    private readonly object _lock = new();

    public double GetChance()
    {
        lock (_lock)
        {
            Console.WriteLine($"---------------------->[ConfigApi] [Inside ConfigHub] Get Error chance: {_errorChance}");
            return _errorChance;
        }
    }

    public void SetChance(double value)
    {
        Console.WriteLine($"---------------------->[ProducerAPI] [Inside ErrorChanceStore] Error chance updated !!! to {value}");
        lock (_lock)
            _errorChance = value;
    }
}