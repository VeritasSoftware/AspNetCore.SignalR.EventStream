namespace AspNetCore.SignalR.EventStream.Models
{
    public class SubscribeModel
    {
        public string StreamName { get; set; } = string.Empty;
        public Guid SubscriberId { get; set; }
        public Guid SubscriberKey { get; set; }
    }
}
