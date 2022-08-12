namespace AspNetCore.SignalR.EventStream.Models
{
    public class UpdateSubscriberModel
    {
        public DateTimeOffset? LastAccessedEventAt { get; set; }
        public Guid? LastAccessedEventAtFromEventId { get; set; }
    }
}
