namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStreamSubscriber
    {
        public long Id { get; set; }
        public long StreamId { get; set; }
        public virtual EventStream Stream { get; set; }
        public Guid SubscriberId { get; set; }
        public Guid SubscribeKey { get; set; }
        public string? ConnectionId { get; set; }
        public string? ReceiveMethod { get; set; }
        public DateTime? LastAccessedEventAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
