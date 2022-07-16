namespace AspNetCore.SignalR.EventStream.Models
{
    public class EventModel
    {
        public string? Type { get; set; }
        public byte[]? Data { get; set; }
        public string? JsonData { get; set; }
        public byte[]? MetaData { get; set; }
        public bool IsJson { get; set; }
        public Guid StreamId { get; set; }
        public string? StreamName { get; set; }        
    }

    public class EventModelResult : EventModel
    {
        public long Id { get; set; }
        public Guid EventId { get; set; }
    }
}
