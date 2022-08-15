namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStreamSubscriber : BaseEntity
    {
        public long StreamId { get; set; }
        public virtual EventStream Stream { get; set; }
        public Guid SubscriberId { get; set; }
        public Guid SubscribeKey { get; set; }
        public string? ConnectionId { get; set; }
        public string? ReceiveMethod { get; set; }
        public long? LastAccessedEventId { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
