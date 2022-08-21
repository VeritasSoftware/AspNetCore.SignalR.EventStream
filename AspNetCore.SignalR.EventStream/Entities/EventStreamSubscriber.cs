namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStreamSubscriber : BaseEntity
    {
        public long StreamId { get; set; }
        public virtual EventStream Stream { get; set; }
        public Guid SubscriberId { get; set; }
        public Guid SubscriberKey { get; set; }
        public string? ConnectionId { get; set; }
        public string? ReceiveMethod { get; set; }
        public long? LastAccessedCurrentEventId { get; set; } = 0;
        public long? LastAccessedFromEventId { get; set; } = 0;
        public long? LastAccessedToEventId { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
