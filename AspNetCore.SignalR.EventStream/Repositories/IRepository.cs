﻿using AspNetCore.SignalR.EventStream.DomainEntities;
using AspNetCore.SignalR.EventStream.Entities;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public interface IRepository
    {
        Task AddAsync(params Event[] @event);
        Task AddAsync(Entities.EventStream eventStream);
        Task AddAsync(EventStreamSubscriber subscriber);
        Task AddAsync(EventStreamAssociation association);
        Task UpdateAsync(Entities.EventStream eventStream);
        Task UpdateSubscriptionLastAccessedAsync(Guid subsciberId, long lastAccessedEvent);

        Task DeleteSubscriptionAsync(Guid subscriptionId, Guid streamId);
        Task DeleteSubscriptionAsync(string connectionId);
        Task DeleteAllSubscriptionsAsync();

        Task<IEnumerable<ActiveSubscription>> GetActiveSubscriptions();
        Task<EventStreamSubscriber> GetSubscriberAsync(Guid subscriberId, long? from = null);
        Task<Event> GetEventAsync(long streamId, long eventId);
        Task<Event> GetEventAsync(long streamId, Guid eventId);
        Task<Entities.EventStream> GetStreamAsync(Guid streamId, DateTime? from = null);
        Task<Entities.EventStream> GetStreamAsync(long streamId, long? from = null);
        Task<Entities.EventStream> GetStreamAsync(string streamName, DateTime? from = null);
        Task<IEnumerable<ActiveAssociatedStreams>> GetAssociatedStreams();

        Task<bool> DoesStreamExistAsync(string name);
        Task<bool> DoesStreamExistAsync(Guid streamId);
        Task<bool> DoesEventStreamAssociationExistAsync(long streamId, long associatedStreamId);

        Task<IEnumerable<Entities.EventStream>> SearchEventStreamsAsync(SearchEventStreamsEntity search);
        Task DeleteEventStreamAsync(long id);
    }
}
