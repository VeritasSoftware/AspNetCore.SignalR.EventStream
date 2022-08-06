namespace AspNetCore.SignalR.EventStream.Models
{
    public class EventStreamModel
    {
        public long Id { get; set; }
        public Guid StreamId { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastEventInsertedAt { get; set; }
        public DateTimeOffset? LastAssociatedAt { get; set; }
    }
}
