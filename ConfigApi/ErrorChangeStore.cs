namespace ConfigApi;

public class ErrorChanceStore
{
    private double _errorChance = 0.3;
    private readonly object _lock = new();

    public double GetChance()
    {
        lock (_lock)
            return _errorChance;
    }

    public void SetChance(double value)
    {
        lock (_lock)
            _errorChance = value;
    }
}