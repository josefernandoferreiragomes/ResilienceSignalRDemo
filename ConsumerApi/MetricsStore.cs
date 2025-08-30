using System.Threading;

namespace ConsumerApi
{
    public class MetricsStore
    {
        private long _totalCalls;
        private long _retryCount;
        private string _circuitState = "Closed";
        private DateTime? _lastReset;

        public long TotalCalls => Interlocked.Read(ref _totalCalls);
        public long RetryCount => Interlocked.Read(ref _retryCount);
        public string CircuitState => _circuitState;
        public DateTime? LastReset => _lastReset;

        public void RecordCall() => Interlocked.Increment(ref _totalCalls);
        public void RecordRetry() => Interlocked.Increment(ref _retryCount);
        public void SetCircuitState(string state) => _circuitState = state;
        public void SetLastReset(DateTime time) => _lastReset = time;
    }
}