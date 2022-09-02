namespace AspNetCore.SignalR.EventStream.Models
{
    public class SubscribeModel
    {
        public string StreamName { get; set; }
        public string Type { get; set; }
        public Guid SubscriberId { get; set; }
        public Guid SubscriberKey { get; set; }
        public int LastAccessedEventId { get; set; }
    }
}
