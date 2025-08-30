namespace ConsumerApi
{
    public class ResilienceConfig
    {
        public int RetryCount { get; set; } = 3;
        public List<int> RetryBackoffMilliseconds { get; set; } = new() { 200, 400, 800 };
        public int ExceptionsAllowedBeforeBreaking { get; set; } = 2;
        public int DurationOfBreakInSeconds { get; set; } = 3;
    }
}