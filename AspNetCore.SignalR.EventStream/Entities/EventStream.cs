namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStream : BaseEntity
    {
        public Guid StreamId { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastEventInsertedAt { get; set; }
        public long? LastAssociatedEventId { get; set; } = 0;
        public virtual ICollection<Event>? Events { get; set; }
    }
}
