using System;
using System.Collections.Generic;

namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStream
    {
        public long Id { get; set; }
        public Guid StreamId { get; set; }
        public string Name { get; set; }
        //public string Type { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public virtual ICollection<Event> Events { get; set; }
    }
}
