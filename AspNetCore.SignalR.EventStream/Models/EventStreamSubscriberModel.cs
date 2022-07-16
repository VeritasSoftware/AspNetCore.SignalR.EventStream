namespace AspNetCore.SignalR.EventStream.Models
{
    public class EventStreamSubscriberModel
    {
        public long Id { get; set; }
        public long StreamId { get; set; }
        public virtual EventStreamModel Stream { get; set; }
        public Guid SubscriberId { get; set; }
        public string? ConnectionId { get; set; }
        public string? ReceiveMethod { get; set; }
        public DateTimeOffset? LastAccessedEventAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
