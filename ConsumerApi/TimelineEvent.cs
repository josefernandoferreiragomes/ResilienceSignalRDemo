using System;

namespace ConsumerApi
{
    public class TimelineEvent
    {
        public DateTime TimestampUtc { get; set; }
        public string Kind { get; set; } = string.Empty; // ConsumerRequest, ProducerResponse, CircuitState
        public int? StatusCode { get; set; }
        public string? State { get; set; } // for circuit state events
        public string? Detail { get; set; }
    }
}
