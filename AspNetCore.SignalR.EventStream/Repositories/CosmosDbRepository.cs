using AspNetCore.SignalR.EventStream.DomainEntities;
using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public class CosmosDbRepository : IRepository
    {
        private readonly CosmosDbContext _context;

        private static object _lock = new object();

        public CosmosDbRepository(CosmosDbContext context)
        {
            _context = context;
        }

        public void EnsureDatabaseDeleted()
        {
            _context.Database.EnsureDeleted();
        }

        public void EnsureDatabaseCreated()
        {
            _context.Database.EnsureCreated();
        }

        public async Task AddAsync(params Event[] events)
        {
            lock (_lock)
            {
                try
                {
                    var maxId = GetNextMaxIdAsync<CosmosEvent>().Result;

                    foreach (var @event in events)
                    {
                        var cosmosEvent = new CosmosEvent
                        {
                            CreatedAt = @event.CreatedAt,
                            Data = @event.Data,
                            EventId = @event.EventId,
                            Id = maxId,
                            IsJson = @event.IsJson,
                            JsonData = @event.JsonData,
                            MetaData = @event.MetaData,
                            OriginalEventId = @event.OriginalEventId,                            
                            StreamId = @event.StreamId,
                            PartitionKey = @event.StreamId.ToString(),
                            Type = @event.Type,
                        };                        

                        _context.Events.Add(cosmosEvent);                        

                        maxId++;
                    }

                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    throw;
                }                
            }
        }

        public async Task AddAsync(Entities.EventStream eventStream)
        {
            var maxId = await GetNextMaxIdAsync<Entities.EventStream>();

            eventStream.Id = maxId;

            _context.EventsStream.Add(eventStream);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(EventStreamSubscriber subscriber)
        {
            subscriber.Stream = null;

            var maxId = await GetNextMaxIdAsync<EventStreamSubscriber>();

            subscriber.Id = maxId;

            _context.Subscribers.Add(subscriber);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(EventStreamAssociation association)
        {
            var maxId = await GetNextMaxIdAsync<EventStreamAssociation>();

            association.Id = maxId;

            _context.EventStreamsAssociation.Add(association);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Entities.EventStream eventStream)
        {
            var stream = await _context.EventsStream.SingleOrDefaultAsync(s => s.Id == eventStream.Id);

            if (stream == null)
            {
                return;
            }

            stream.Name = eventStream.Name;
            stream.LastAssociatedEventId = eventStream.LastAssociatedEventId;

            _context.EventsStream.Update(stream);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubscriptionLastAccessedAsync(Guid subsciberId, long lastAccessedEvent)
        {
            var subscriber = await _context.Subscribers.FirstOrDefaultAsync(s => s.SubscriberId == subsciberId);
            if (subscriber != null)
            {
                subscriber.LastAccessedEventId = lastAccessedEvent;
                _context.Subscribers.Update(subscriber);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteSubscriptionAsync(Guid subscriptionId, Guid streamId)
        {
            var subscription = await _context.Subscribers
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
            var client = _context.Database.GetCosmosClient();

            var dbName = _context.Database.GetDbConnection().Database;

            Container container = client.GetContainer(dbName, "Subscribers");
            ResponseMessage response = await container.ReplaceContainerStreamAsync(new ContainerProperties());

            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> DoesStreamExistAsync(string name)
        {
            return await _context.EventsStream.CountAsync(s => s.Name == name) > 0;
        }

        public async Task<bool> DoesStreamExistAsync(Guid streamId)
        {
            return await _context.EventsStream.CountAsync(s => s.StreamId == streamId) > 0;
        }

        public async Task<bool> DoesEventStreamAssociationExistAsync(long streamId, long associatedStreamId)
        {
            return await _context.EventStreamsAssociation.CountAsync(s => s.StreamId == streamId && s.AssociatedStreamId == associatedStreamId) > 0;
        }

        public async Task<IEnumerable<Entities.EventStream>> SearchEventStreamsAsync(SearchEventStreamsEntity search)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteEventStreamAsync(long id)
        {
            var stream = await _context.EventsStream.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id);

            if (stream != null)
            {
                try
                {
                    var associations = await _context.EventStreamsAssociation.Where(sa => sa.AssociatedStreamId == id).ToListAsync();

                    foreach (var association in associations)
                    {
                        _context.EventStreamsAssociation.Remove(association);
                    }

                    await _context.SaveChangesAsync();

                    _context.EventsStream.Remove(stream);
                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<Event> GetEventAsync(long streamId, long eventId)
        {
            var @event = await _context.Events.WithPartitionKey(streamId.ToString()).AsNoTracking()
                                              .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event != null)
            {
                @event.Stream = await _context.EventsStream.SingleAsync(s => s.Id == @event.StreamId);
            }

            return @event;
        }

        public async Task<Event> GetEventAsync(long streamId, Guid eventId)
        {
            var @event = await _context.Events.WithPartitionKey(streamId.ToString()).AsNoTracking()
                                              .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (@event != null)
            {
                @event.Stream = await _context.EventsStream.SingleAsync(s => s.Id == @event.StreamId);
            }

            return @event;
        }

        public async Task<Entities.EventStream> GetStreamAsync(Guid streamId, DateTime? from = null)
        {
            var eventStream = await _context.EventsStream.AsNoTracking()
                                                          .FirstOrDefaultAsync(es => es.StreamId == streamId);

            if (from.HasValue)
            {
                var events = await _context.Events.WithPartitionKey(eventStream.Id.ToString()).AsNoTracking()
                                                  .Where(e => e.CreatedAt > from.Value)
                                                  .ToListAsync();

                eventStream.Events = events.OrderBy(e => e.CreatedAt)
                                           .Select(e => new Event
                                           {
                                               Id = e.Id,
                                               CreatedAt = e.CreatedAt,
                                               Data = e.Data,
                                               EventId = e.EventId,
                                               IsJson = e.IsJson,
                                               JsonData = e.JsonData,
                                               MetaData = e.MetaData,
                                               OriginalEventId = e.OriginalEventId,
                                               StreamId = e.StreamId,
                                               Type = e.Type
                                           })
                                           .ToList();
            }
            else if (eventStream != null)
            {
                var events = await _context.Events.WithPartitionKey(eventStream.Id.ToString()).AsNoTracking()
                                                  .ToListAsync();

                eventStream.Events = events.OrderBy(e => e.CreatedAt)
                                           .Select(e => new Event
                                           {
                                               Id = e.Id,
                                               CreatedAt = e.CreatedAt,
                                               Data = e.Data,
                                               EventId = e.EventId,
                                               IsJson = e.IsJson,
                                               JsonData = e.JsonData,
                                               MetaData = e.MetaData,
                                               OriginalEventId = e.OriginalEventId,
                                               StreamId = e.StreamId,
                                               Type = e.Type
                                           })
                                           .ToList();
            }

            return eventStream;
        }

        public async Task<Entities.EventStream> GetStreamAsync(long streamId, long? from = null)
        {
            Entities.EventStream eventStream = null;
            bool proceed = false;

            lock(_lock)
            {
                proceed = true;
            }

            if (proceed)
            {
                if (from.HasValue)
                {
                    eventStream = await _context.EventsStream.AsNoTracking()
                                                       .FirstOrDefaultAsync(es => es.Id == streamId);

                    var events = await _context.Events.WithPartitionKey(streamId.ToString()).AsNoTracking()
                                                      .Where(e => e.Id > from.Value)
                                                      .ToListAsync();

                    eventStream.Events = events.OrderBy(e => e.CreatedAt)
                                               .Select(e => new Event
                                               {
                                                   Id = e.Id,
                                                   CreatedAt = e.CreatedAt,
                                                   Data = e.Data,
                                                   EventId = e.EventId,
                                                   IsJson = e.IsJson,
                                                   JsonData = e.JsonData,
                                                   MetaData = e.MetaData,
                                                   OriginalEventId = e.OriginalEventId,
                                                   StreamId = e.StreamId,
                                                   Type = e.Type
                                               })
                                               .ToList();
                }
                else
                {
                    eventStream = await _context.EventsStream.AsNoTracking()
                                                       .FirstOrDefaultAsync(es => es.Id == streamId);
                }
            }
            
            return eventStream;
        }

        public async Task<Entities.EventStream> GetStreamAsync(string streamName, DateTime? from = null)
        {
            streamName = streamName.Trim().ToLower();

            var eventStream = await _context.EventsStream.AsNoTracking()
                                                         .FirstOrDefaultAsync(es => es.Name.ToLower() == streamName);

            if (from.HasValue)
            {
                var events = await _context.Events.WithPartitionKey(eventStream.Id.ToString()).AsNoTracking()
                                                  .Where(e => e.CreatedAt > from.Value)
                                                  .ToListAsync();

                eventStream.Events = events.OrderBy(e => e.CreatedAt)
                                           .Select(e => new Event
                                           {
                                               Id = e.Id,
                                               CreatedAt = e.CreatedAt,
                                               Data = e.Data,
                                               EventId = e.EventId,
                                               IsJson = e.IsJson,
                                               JsonData = e.JsonData,
                                               MetaData = e.MetaData,
                                               OriginalEventId = e.OriginalEventId,
                                               StreamId = e.StreamId,
                                               Type = e.Type
                                           })
                                           .ToList();
            }
            else if (eventStream != null)
            {
                var events = await _context.Events.WithPartitionKey(eventStream.Id.ToString()).AsNoTracking()
                                                  .ToListAsync();

                eventStream.Events = events.OrderBy(e => e.CreatedAt)
                                           .Select(e => new Event
                                           {
                                               Id = e.Id,
                                               CreatedAt = e.CreatedAt,
                                               Data = e.Data,
                                               EventId = e.EventId,
                                               IsJson = e.IsJson,
                                               JsonData = e.JsonData,
                                               MetaData = e.MetaData,
                                               OriginalEventId = e.OriginalEventId,
                                               StreamId = e.StreamId,
                                               Type = e.Type
                                           })
                                           .ToList();
            }

            return eventStream;
        }

        public async Task<IEnumerable<ActiveSubscription>> GetActiveSubscriptions()
        {
            var subscribers = await _context.Subscribers.AsNoTracking().ToListAsync();

            var streamIds = subscribers.Select(s => s.StreamId);

            var streams = await _context.EventsStream.Where(s => streamIds.Contains(s.Id)).ToListAsync();

            return subscribers.Select(s => new ActiveSubscription
            {
                StreamId = streams.Single(st => st.Id == s.StreamId).StreamId,
                SubscriptionId = s.SubscriberId
            });
        }

        public async Task<IEnumerable<ActiveAssociatedStreams>> GetAssociatedStreams()
        {
            var associations = await _context.EventStreamsAssociation.ToListAsync();
            return associations.GroupBy(s => s.StreamId)
                                .Select(s => new ActiveAssociatedStreams
                                {
                                    StreamId = s.Key,
                                    AssociatedStreamIds = s.Select(z => z.AssociatedStreamId)
                                });
        }

        public async Task<EventStreamSubscriber> GetSubscriberAsync(Guid subscriberId, long? from = null)
        {
            if (from.HasValue)
            {
                var id = from.Value;

                var subscriber = await _context.Subscribers.AsNoTracking().FirstOrDefaultAsync(s => s.SubscriberId == subscriberId);

                if (subscriber != null)
                {
                    lock (_lock)
                    {
                        subscriber.Stream = _context.EventsStream.AsNoTracking().FirstOrDefault(s => s.Id == subscriber.StreamId);
                        var streamId = subscriber.StreamId;

                        var events = _context.Events.WithPartitionKey(streamId.ToString()).AsNoTracking()
                                                    .Where(e => e.Id > id)
                                                    .ToList();

                        events = events.OrderBy(e => e.Id).ToList();

                        subscriber.Stream.Events = events.Select(e => new Event
                        {
                            Id = e.Id,
                            CreatedAt = e.CreatedAt,
                            Data = e.Data,
                            EventId = e.EventId,
                            IsJson = e.IsJson,
                            JsonData = e.JsonData,
                            MetaData = e.MetaData,
                            OriginalEventId = e.OriginalEventId,
                            StreamId = e.StreamId,
                            Type = e.Type
                        }).ToList();
                    }
                }

                return subscriber;
            }
            else
            {
                var subscriber = await _context.Subscribers.AsNoTracking().FirstOrDefaultAsync(s => s.SubscriberId == subscriberId);

                if (subscriber != null)
                    subscriber.Stream = await _context.EventsStream.AsNoTracking().FirstOrDefaultAsync(s => s.Id == subscriber.StreamId);

                return subscriber;
            }
        }

        private async Task<long> GetNextMaxIdAsync<T>()
            where T : BaseEntity
        {
            long maxId = 0;

            try
            {
                maxId = await _context.Set<T>().MaxAsync(o => o.Id);
            }
            catch (Exception)
            {}

            return ++maxId;
        }
    }
}
