namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStream
    {
        public long Id { get; set; }
        public Guid StreamId { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? LastEventInsertedAt { get; set; }
        public DateTimeOffset? LastAssociatedAt { get; set; }
        public virtual ICollection<Event>? Events { get; set; }
    }
}
