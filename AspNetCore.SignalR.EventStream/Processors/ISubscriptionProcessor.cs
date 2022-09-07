
namespace AspNetCore.SignalR.EventStream.Processors
{
    public interface ISubscriptionProcessor
    {
        string? EventStreamHubUrl { get; set; }
        string Name { get; }
        string? SecretKey { get; set; }
        bool Start { get; set; }

        ValueTask DisposeAsync();
        Task ProcessSubscriber(Guid subscriberId);
    }
}