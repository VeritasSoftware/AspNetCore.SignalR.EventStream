namespace AspNetCore.SignalR.EventStream.Models
{
    public class EventStreamModel
    {
        public long Id { get; set; }
        public Guid StreamId { get; set; }        
        public string Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<EventModelResult> Events { get; set; }
    }
}
