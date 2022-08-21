namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStream : BaseEntity
    {
        public Guid StreamId { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public long? LastAssociatedEventId { get; set; } = null;
        public virtual ICollection<Event>? Events { get; set; }
    }
}
