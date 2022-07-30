namespace AspNetCore.SignalR.EventStream.Models
{
    public class AssociateStreamsModel
    {
        public string? Name { get; set; }
        public Guid StreamId { get; set; }
        public bool Existing { get; set; }
        public IEnumerable<AssociatedStreamModel> AssociatedStreams { get; set; } = Enumerable.Empty<AssociatedStreamModel>();
    }

    public class AssociatedStreamModel
    {
        public string? Name { get; set; }
        public Guid StreamId { get; set; }
    }
}
