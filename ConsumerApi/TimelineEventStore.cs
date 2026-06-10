using System.Collections.Concurrent;

namespace ConsumerApi
{
    public class TimelineEventStore
    {
        private readonly ConcurrentQueue<TimelineEvent> _events = new();
        private readonly TimeSpan _retention = TimeSpan.FromMinutes(60); // keep events for up to 60 minutes

        public void Add(TimelineEvent ev)
        {
            _events.Enqueue(ev);
            PruneOld();
        }

        private void PruneOld()
        {
            var cutoff = DateTime.UtcNow - _retention;
            while (_events.TryPeek(out var peek) && peek.TimestampUtc < cutoff)
            {
                _events.TryDequeue(out _);
            }
        }

        public IEnumerable<TimelineEvent> GetEvents(DateTime fromUtc)
        {
            return _events.Where(e => e.TimestampUtc >= fromUtc).ToList();
        }
    }
}
