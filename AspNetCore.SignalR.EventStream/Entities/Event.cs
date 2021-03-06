using System;

namespace AspNetCore.SignalR.EventStream.Entities
{
    public class Event
    {
        public long Id { get; set; }
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string? Type { get; set; }
        public long StreamId { get; set; }
        public virtual EventStream? Stream { get; set; }
        public byte[]? Data { get; set; }
        public string? JsonData { get; set; }
        public byte[]? MetaData { get; set; }
        public bool IsJson { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
