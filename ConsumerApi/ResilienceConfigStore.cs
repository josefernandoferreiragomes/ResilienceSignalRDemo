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
                return _config;
            }
        }

        public void UpdateConfig(ResilienceConfig config)
        {
            lock (_lock)
            {
                _config = config;
            }
        }
    }
}