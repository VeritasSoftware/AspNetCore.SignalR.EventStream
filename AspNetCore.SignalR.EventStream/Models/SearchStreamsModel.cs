namespace AspNetCore.SignalR.EventStream.Models
{
    public class SearchStreamsModel
    {
        public Guid? StreamId { get; set; }
        public string? Name { get; set; }
        public DateTimeOffset? CreatedStart { get; set; }
        public DateTimeOffset? CreatedEnd { get; set; }
    }
}
