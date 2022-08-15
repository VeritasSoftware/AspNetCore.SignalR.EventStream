namespace AspNetCore.SignalR.EventStream.Entities
{
    public class Event : BaseEntity
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string? Type { get; set; }
        public long StreamId { get; set; }
        public virtual EventStream? Stream { get; set; }
        public byte[]? Data { get; set; }
        public string? JsonData { get; set; }
        public byte[]? MetaData { get; set; }
        public bool IsJson { get; set; }
        public Guid? OriginalEventId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
    
    public class CosmosEvent : Event
    {        
        public string PartitionKey { get; set; }

        public CosmosEvent()
        {
            this.PartitionKey = this.StreamId.ToString();
        }
    }
}
