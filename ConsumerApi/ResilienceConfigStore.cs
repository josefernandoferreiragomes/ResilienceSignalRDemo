using OpenTelemetry.Metrics;

namespace ConsumerApi
{
    public class ResilienceConfigStore
    {
        private ResilienceConfig _config;
        private readonly object _lock = new();

        public ResilienceConfigStore(ResilienceConfig initialConfig) => _config = initialConfig;

        public ResilienceConfig GetConfig()
        {
            lock (_lock)
            {
                Console.WriteLine($"---------------------->[ConsumerApi] [From ConfigStore] GetConfig: {System.Text.Json.JsonSerializer.Serialize(_config)}.");
                return _config;
            }
        }

        public void UpdateConfig(ResilienceConfig config)
        {
            Console.WriteLine($"---------------------->[ConsumerApi] [From ConfigStore] Config update !!! received: {System.Text.Json.JsonSerializer.Serialize(config)}.");
            lock (_lock)
            {
                _config = config;
            }
        }
    }
}