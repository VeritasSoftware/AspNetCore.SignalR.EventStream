namespace AspNetCore.SignalR.EventStream
{
    public class ActiveAssociatedStreams
    {
        public long StreamId { get; set; }
        public IEnumerable<long>? AssociatedStreamIds { get; set; }
    }
}
