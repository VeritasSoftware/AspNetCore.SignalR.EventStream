namespace AspNetCore.SignalR.EventStream.Entities
{
    public class EventStreamAssociation
    {
        public long Id { get; set; }
        public long StreamId { get; set; }
        public virtual EventStream? Stream { get; set; }
        public long AssociatedStreamId { get; set; }
        public virtual EventStream? AssociatedStream { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
