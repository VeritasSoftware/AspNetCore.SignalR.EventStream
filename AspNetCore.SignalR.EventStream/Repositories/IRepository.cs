using AspNetCore.SignalR.EventStream.DomainEntities;
using AspNetCore.SignalR.EventStream.Entities;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public interface IRepository
    {
        Task AddAsync(params Event[] events);
        Task AddAsync(Entities.EventStream eventStream);
        Task AddAsync(EventStreamSubscriber subscriber);
        Task AddAsync(EventStreamAssociation association);
        Task UpdateAsync(Entities.EventStream eventStream);
        Task UpdateAsync(EventStreamSubscriber subscriber);
        Task UpdateSubscriptionLastAccessedAsync(Guid subsciberId, long? lastAccessedCurrentEventId);

        Task DeleteSubscriptionAsync(Guid subscriptionId, Guid streamId);
        Task DeleteSubscriptionAsync(string connectionId);
        Task DeleteAllSubscriptionsAsync();

        void EnsureDatabaseDeleted();
        void EnsureDatabaseCreated();

        Task<IEnumerable<ActiveSubscription>> GetActiveSubscriptionsAsync();
        Task<EventStreamSubscriber?> GetSubscriberAsync(Guid subscriberId, long? fromEventId = null, long? toEventId = null);
        Task<Event> GetEventAsync(long streamId, long eventId);
        Task<Event> GetEventAsync(long streamId, Guid eventId);
        Task<Entities.EventStream> GetStreamAsync(Guid streamId);
        Task<Entities.EventStream> GetStreamAsync(long streamId, long? fromEventId = null);
        Task<Entities.EventStream> GetStreamAsync(string streamName);
        Task<IEnumerable<Entities.EventStream>> GetStreamsAsync(string streamName);
        Task<IEnumerable<ActiveAssociatedStreams>> GetAssociatedStreamsAsync();

        Task<bool> DoesStreamExistAsync(string name);
        Task<bool> DoesStreamExistAsync(Guid streamId);
        Task<bool> DoesEventStreamAssociationExistAsync(long streamId, long associatedStreamId);

        Task<IEnumerable<Entities.EventStream>> SearchEventStreamsAsync(SearchEventStreamsEntity search);
        Task DeleteEventStreamAsync(long id);
    }
}
