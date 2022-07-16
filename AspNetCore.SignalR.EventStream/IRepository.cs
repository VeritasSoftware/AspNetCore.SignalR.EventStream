using AspNetCore.SignalR.EventStream.Entities;

namespace AspNetCore.SignalR.EventStream
{
    public interface IRepository
    {
        Task AddAsync(Event @event);
        Task AddAsync(Entities.EventStream eventStream);
        Task AddAsync(EventStreamSubscriber subscriber);
        Task UpdateSubscriptionLastAccessed(Guid subsciberId, DateTimeOffset lastAccessed);

        Task DeleteSubscriptionAsync(Guid subscriptionId, Guid streamId);

        Task<IEnumerable<ActiveSubscription>> GetActiveSubscriptions();
        Task<EventStreamSubscriber> GetSubscriberAsync(Guid subscriberId, Guid streamId, DateTime? from = null);
        Task<Entities.EventStream> GetStreamAsync(Guid streamId, DateTime? from = null);
        Task<Entities.EventStream> GetStreamAsync(string streamName, DateTime? from = null);
    }
}
