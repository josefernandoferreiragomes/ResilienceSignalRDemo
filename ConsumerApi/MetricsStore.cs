using System.Threading;

namespace ConsumerApi
{
    public class MetricsStore
    {
        private long _totalCalls;
        private long _retryCount;
        // use Interlocked.Exchange for the string to ensure safe updates
        private string _circuitState = "Closed";
        // store ticks in a long so we can use Interlocked on it
        private long _lastResetTicks = -1;
        // count circuit open events
        private long _circuitOpenCount;

        public long TotalCalls => Interlocked.Read(ref _totalCalls);
        public long RetryCount => Interlocked.Read(ref _retryCount);
        public string CircuitState => _circuitState;
        public DateTime? LastReset
        {
            get
            {
                var ticks = Interlocked.Read(ref _lastResetTicks);
                return ticks == -1 ? (DateTime?)null : new DateTime(ticks, DateTimeKind.Utc);
            }
        }

        public long CircuitOpenCount => Interlocked.Read(ref _circuitOpenCount);

        public void RecordCall() => Interlocked.Increment(ref _totalCalls);
        public void RecordRetry() => Interlocked.Increment(ref _retryCount);
        public void RecordCircuitOpen() => Interlocked.Increment(ref _circuitOpenCount);
        public void SetCircuitState(string state) => System.Threading.Interlocked.Exchange(ref _circuitState, state);
        public void SetLastReset(DateTime time) => Interlocked.Exchange(ref _lastResetTicks, time.ToUniversalTime().Ticks);
    }
}