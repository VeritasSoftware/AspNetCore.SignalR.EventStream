namespace AspNetCore.SignalR.EventStream.Models
{
    public class UpdateSubscriberModel
    {
        public long? LastAccessedFromEventId { get; set; } = 0;
        public long? LastAccessedToEventId { get; set; } = 0;
    }
}
