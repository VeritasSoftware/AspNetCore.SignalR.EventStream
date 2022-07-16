namespace AspNetCore.SignalR.EventStream
{
    public class ActiveSubscription
    {
        public Guid SubscriptionId { get; set; }
        public Guid StreamId { get; set; }
    }
}
