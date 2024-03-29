﻿using AspNetCore.SignalR.EventStream.DomainEntities;
using AspNetCore.SignalR.EventStream.Entities;
using AspNetCore.SignalR.EventStream.Processors;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public class SqliteRepository : IRepository
    {
        private readonly SqliteDbContext _context;
        private readonly ISubscriptionProcessorNotifier _subscriptionProcessorNotifier;
        private readonly IAssociateStreamProcessorNotifier _associateStreamProcessorNotifier;

        private static object _lock = new object();

        public SqliteRepository(SqliteDbContext context, ISubscriptionProcessorNotifier subscriptionProcessorNotifier, IAssociateStreamProcessorNotifier associateStreamProcessorNotifier)
        {
            _context = context;
            _subscriptionProcessorNotifier = subscriptionProcessorNotifier;
            _associateStreamProcessorNotifier = associateStreamProcessorNotifier;
        }

        public void EnsureDatabaseDeleted()
        {
            _context.Database.EnsureDeleted();
        }

        public void EnsureDatabaseCreated()
        {
            _context.Database.EnsureCreated();
            _context.Database.Migrate();
        }

        public async Task AddAsync(params Event[] events)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.Events.AddRange(events);

                    await _context.SaveChangesAsync();

                    var eventIds = events.Select(e => e.EventId).ToList();

                    var savedEvents = await _context.Events.Where(e => eventIds.Contains(e.EventId)).OrderBy(e => e.Id).ToListAsync();

                    lock (_lock)
                    {
                        transaction.Commit();                        

                        _subscriptionProcessorNotifier.OnEventsAddedAsync(savedEvents).Wait();                        
                    }

                    await _associateStreamProcessorNotifier.OnEventsAddedAsync(savedEvents);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

        }

        public async Task AddAsync(Entities.EventStream eventStream)
        {
            _context.EventsStream.Add(eventStream);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(EventStreamSubscriber subscriber)
        {
            subscriber.Stream = null;
            _context.Subscribers.Add(subscriber);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(EventStreamAssociation association)
        {
            _context.EventStreamsAssociation.Add(association);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Entities.EventStream eventStream)
        {
            var stream = await _context.EventsStream.SingleOrDefaultAsync(s => s.Id == eventStream.Id);

            if (stream == null)
                throw new InvalidOperationException($"Stream {eventStream.Id} not found.");

            stream.Name = eventStream.Name;
            stream.LastAssociatedEventId= eventStream.LastAssociatedEventId;

            _context.EventsStream.Update(stream);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EventStreamSubscriber subscriber)
        {
            var subscriberEntity = await _context.Subscribers.FirstOrDefaultAsync(s => s.SubscriberId == subscriber.SubscriberId);
            if (subscriberEntity == null)
                throw new InvalidOperationException($"Subscriber {subscriber.SubscriberId} not found.");

            subscriberEntity.LastAccessedCurrentEventId = subscriber.LastAccessedFromEventId;
            subscriberEntity.LastAccessedFromEventId = subscriber.LastAccessedFromEventId;
            subscriberEntity.LastAccessedToEventId = subscriber.LastAccessedToEventId;

            _context.Subscribers.Update(subscriberEntity);
            
            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubscriptionLastAccessedAsync(Guid subsciberId, long? lastAccessedCurrentEventId)
        {
            var subscriber = await _context.Subscribers.FirstOrDefaultAsync(s => s.SubscriberId == subsciberId);
            if (subscriber != null)
            {
                subscriber.LastAccessedCurrentEventId = lastAccessedCurrentEventId;
                _context.Subscribers.Update(subscriber);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteSubscriptionAsync(Guid subscriptionId, Guid streamId)
        {
            var subscription = await _context.Subscribers.Include(s => s.Stream)
                                             .FirstOrDefaultAsync(s => (s.Stream.StreamId == streamId) && (s.SubscriberId == subscriptionId));

            if (subscription != null)
            {
                _context.Remove(subscription);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteSubscriptionAsync(string connectionId)
        {
            var subscription = await _context.Subscribers
                                             .FirstOrDefaultAsync(s => s.ConnectionId == connectionId);

            if (subscription != null)
            {
                _context.Remove(subscription);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAllSubscriptionsAsync()
        {
            await _context.Database.ExecuteSqlRawAsync(@"delete from Subscribers;    
                                                         delete from sqlite_sequence where name = 'Subscribers'; ");
        }

        public async Task<bool> DoesStreamExistAsync(string name)
        {
            return await _context.EventsStream.AnyAsync(s => s.Name == name);
        }

        public async Task<bool> DoesStreamExistAsync(Guid streamId)
        {
            return await _context.EventsStream.AnyAsync(s => s.StreamId == streamId);
        }

        public async Task<bool> DoesEventStreamAssociationExistAsync(long streamId, long associatedStreamId)
        {
            return await _context.EventStreamsAssociation.AnyAsync(s => s.StreamId == streamId && s.AssociatedStreamId == associatedStreamId);
        }

        public async Task<IEnumerable<Entities.EventStream>> SearchEventStreamsAsync(SearchEventStreamsEntity search)
        {
            return await _context.EventsStream.Where(builder => builder.Initial(x => true)
                                                                       .And(!string.IsNullOrEmpty(search.Name), x => x.Name.ToLower().Contains(search.Name.ToLower()))
                                                                       .And(search.StreamId.HasValue, x => x.StreamId == search.StreamId.Value)
                                                                       .And(search.CreatedStart.HasValue, x => x.CreatedAt >= search.CreatedStart.Value)
                                                                       .And(search.CreatedEnd.HasValue, x => x.CreatedAt <= search.CreatedEnd.Value)
                                                                       .ToExpressionPredicate()).ToListAsync();
        }

        public async Task<IEnumerable<Event>> SearchEventsAsync(SearchEventsEntity search)
        {
            return await _context.Events.Include(e => e.Stream).Where(builder => builder.Initial(x => x.StreamId == search.StreamId)
                                                               .And(!string.IsNullOrEmpty(search.Type), x => x.Type.ToLower().Contains(search.Type.ToLower()))
                                                               .And(search.CreatedStart.HasValue, x => x.CreatedAt >= search.CreatedStart.Value)
                                                               .And(search.CreatedEnd.HasValue, x => x.CreatedAt <= search.CreatedEnd.Value)
                                                               .ToExpressionPredicate())
                                                               .Take(search.MaxReturnRecords.Value)
                                                               .ToListAsync();            
        }

        public async Task DeleteEventStreamAsync(long id)
        {
            var stream = await _context.EventsStream.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);

            if (stream != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var associations = await _context.EventStreamsAssociation.Where(sa => sa.AssociatedStreamId == id).ToListAsync();

                    _context.EventStreamsAssociation.RemoveRange(associations);
                    await _context.SaveChangesAsync();

                    _context.EventsStream.Remove(stream);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<Event> GetEventAsync(long streamId, Guid eventId)
        {
            return await _context.Events.Include(e => e.Stream).AsNoTracking().FirstOrDefaultAsync(e => e.StreamId == streamId && e.EventId == eventId);
        }

        public async Task<Entities.EventStream> GetStreamAsync(Guid streamId)
        {
            var eventStream = await _context.EventsStream.AsNoTracking()
                                                          .FirstOrDefaultAsync(es => es.StreamId == streamId);

            return eventStream;
        }

        public async Task<Entities.EventStream> GetStreamAsync(long streamId, long? fromEventId = null)
        {
            Entities.EventStream eventStream;

            if (fromEventId.HasValue)
            {                
                eventStream = await _context.EventsStream.AsNoTracking()
                                                         .Include(es => es.Events.Where(e => e.Id > fromEventId.Value).OrderBy(e => e.Id)).AsNoTracking()
                                                         .FirstOrDefaultAsync(es => es.Id == streamId);
            }
            else
            {
                eventStream = await _context.EventsStream.AsNoTracking().FirstOrDefaultAsync(es => es.Id == streamId);
            }

            return eventStream;
        }

        public async Task<Entities.EventStream> GetStreamAsync(string streamName)
        {
            streamName = streamName.Trim().ToLower();

            var eventStream = await _context.EventsStream.AsNoTracking()
                                                         .FirstOrDefaultAsync(es => es.Name.ToLower() == streamName);

            return eventStream;
        }

        public async Task<IEnumerable<Entities.EventStream>> GetStreamsAsync(string streamName)
        {
            streamName = streamName.Trim().ToLower();

            var eventStreams = await _context.EventsStream.AsNoTracking()
                                                         .Where(es => es.Name.ToLower().Contains(streamName))
                                                         .ToListAsync();

            return eventStreams;
        }

        public async Task<IEnumerable<EventStreamSubscriber>> GetActiveSubscriptionsAsync(long streamId)
        {
            return await _context.Subscribers.Include(s => s.Stream)
                                             .Where(s => s.StreamId == streamId)
                                             .ToListAsync();
        }

        public async Task<IEnumerable<ActiveAssociatedStreams>> GetAssociatedStreamsAsync(long streamId)
        {
            return await _context.EventStreamsAssociation.AsNoTracking().Include(s => s.Stream)
                                                         .Include(s => s.AssociatedStream)
                                                         .Where(s => s.AssociatedStreamId == streamId)
                                                         .GroupBy(s => s.StreamId, (x, y) => new ActiveAssociatedStreams
                                                         {
                                                             StreamId = x,
                                                             AssociatedStreamIds = y.Select(z => z.AssociatedStreamId)
                                                         })
                                                         .ToListAsync();
        }

        public async Task<EventStreamSubscriber?> GetSubscriberAsync(Guid subscriberId, long? fromEventId = null, long? toEventId = null)
        {
            if (fromEventId.HasValue)
            {
                fromEventId = fromEventId ?? 0;
                var fromId = fromEventId.Value;

                var subscriber = await _context.Subscribers.AsNoTracking().Include(s => s.Stream)
                                                           .Include(s => s.Stream.Events.Where(e => e.Id > fromId).OrderBy(e => e.Id))
                                                           .AsNoTracking().FirstOrDefaultAsync(s => s.SubscriberId == subscriberId);
                if (subscriber == null)
                    return null;

                var events = subscriber.Stream.Events;
                if (events.Any())
                {
                    var lastAccessedCurrentEventId = events.Last().Id;

                    await UpdateSubscriptionLastAccessedAsync(subscriberId, lastAccessedCurrentEventId);

                    if (toEventId.HasValue && toEventId.Value > 0)
                    {
                        subscriber.Stream.Events = events.Where(e => e.Id > fromId && e.Id <= toEventId.Value).ToList();
                    }
                }                                

                return subscriber;
            }
            else
            {
                var subscriber = await _context.Subscribers.Include(s => s.Stream).AsNoTracking()
                                                           .FirstOrDefaultAsync(s => s.SubscriberId == subscriberId);

                return subscriber;
            }
        }
    }
}
