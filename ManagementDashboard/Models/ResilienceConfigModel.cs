namespace ManagementDashboard.Models;

public class ResilienceConfigModel
{
    public int RetryCount { get; set; }
    public List<int> RetryBackoffMilliseconds { get; set; } = new();
    public int ExceptionsAllowedBeforeBreaking { get; set; }
    public int DurationOfBreakInSeconds { get; set; }
}