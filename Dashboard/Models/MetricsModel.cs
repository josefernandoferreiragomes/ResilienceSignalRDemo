namespace Dashboard.Models;

public class MetricsModel
{
    public long TotalCalls { get; set; }
    public long RetryCount { get; set; }
    public string CircuitState { get; set; } = "";
    public DateTime? LastReset { get; set; }
}