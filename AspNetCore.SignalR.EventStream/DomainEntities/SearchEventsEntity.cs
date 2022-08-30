namespace AspNetCore.SignalR.EventStream.DomainEntities
{
    public class SearchEventsEntity
    {
        public int StreamId { get; set; }
        public DateTimeOffset? CreatedStart { get; set; }
        public DateTimeOffset? CreatedEnd { get; set; }
        public string? Type { get; set; }
        public int? MaxReturnRecords { get; set; } = 50;
    }
}
