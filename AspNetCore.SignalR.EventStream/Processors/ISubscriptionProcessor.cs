
namespace AspNetCore.SignalR.EventStream.Processors
{
    public interface ISubscriptionProcessor
    {
        string? EventStreamHubUrl { get; set; }
        string Name { get; }
        string? SecretKey { get; set; }
        bool Start { get; set; }
        Task ProcessSubscriber(Guid subscriberId);
    }
}