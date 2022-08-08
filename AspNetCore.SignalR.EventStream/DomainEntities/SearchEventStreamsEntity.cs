namespace AspNetCore.SignalR.EventStream.DomainEntities
{
    public class SearchEventStreamsEntity
    {
        public string? Name { get; set; }
        public Guid? StreamId { get; set; }
        public DateTimeOffset? CreatedBefore { get; set; }
    }
}
