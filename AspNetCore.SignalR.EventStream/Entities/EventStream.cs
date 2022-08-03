namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStream
    {
        public long Id { get; set; }
        public Guid StreamId { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastEventInsertedAt { get; set; }
        public DateTimeOffset? LastAssociatedAt { get; set; }
        public virtual ICollection<Event>? Events { get; set; }
    }
}
