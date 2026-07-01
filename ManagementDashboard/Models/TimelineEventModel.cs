using System;

namespace ManagementDashboard.Models
{
    public class TimelineEventModel
    {
        public DateTime TimestampUtc { get; set; }
        public string Kind { get; set; } = string.Empty;
        public int? StatusCode { get; set; }
        public string? State { get; set; }
        public string? Detail { get; set; }
    }
}
