using System;
using System.Collections.Generic;

namespace AspNetCore.SignalR.EventStream.Models
{
    public class EventStreamModel
    {
        public long Id { get; set; }
        public Guid StreamId { get; set; }        
        public string Name { get; set; }
        public string Type { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public virtual ICollection<EventModelResult> Events { get; set; }
    }
}
