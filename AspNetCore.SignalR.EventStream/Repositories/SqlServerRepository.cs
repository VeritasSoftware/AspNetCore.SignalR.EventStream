using AspNetCore.SignalR.EventStream.DomainEntities;
using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream.Repositories
{
    public class SqlServerRepository : IRepository
    {
        private readonly SqlServerDbContext _context;

        private static object _lock = new object();

        public SqlServerRepository(SqlServerDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(params Event[] @event)
        {            
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Events.AddRange(@event);

                    await _context.SaveChangesAsync();

                    var stream = _context.EventsStream.FirstOrDefault(s => s.Id == @event.First().StreamId);
                    if (stream != null)
                    {
                        stream.LastEventInsertedAt = DateTime.UtcNow;

                        _context.EventsStream.Update(stream);

                        await _context.SaveChangesAsync();
                    }

                    lock (_lock)
                    {
                        transaction.Commit();
                    }                    
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
            {
                return;
            }

            stream.LastEventInsertedAt = eventStream.LastEventInsertedAt;
            stream.Name = eventStream.Name;
            stream.LastAssociatedAt = eventStream.LastAssociatedAt;

            _context.EventsStream.Update(stream);

            await _context.SaveChangesAsync();
        }

        public async Task UpdateSubscriptionLastAccessedAsync(Guid subsciberId, DateTimeOffset lastAccessed)
        {
            var subscriber = await _context.Subscribers.FirstOrDefaultAsync(s => s.SubscriberId == subsciberId);
            if (subscriber != null)
            {
                subscriber.LastAccessedEventAt = lastAccessed;
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
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE dbo.Subscribers");
        }

        public async Task<bool> DoesStreamExistAsync (string name)
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

        public async Task<Event> GetEventAsync(Guid eventId)
        {
            return await _context.Events.Include(e => e.Stream).AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<Entities.EventStream> GetStreamAsync(Guid streamId, DateTime? from = null)
        {
            try
            {
                var eventStream = await _context.EventsStream.AsNoTracking()
                                                          .FirstOrDefaultAsync(es => es.StreamId == streamId);

                if (from.HasValue)
                {
                    eventStream.Events = await _context.Events.AsNoTracking()
                                                        .Where(e => (e.StreamId == eventStream.Id) && (e.CreatedAt > from.Value))
                                                        .OrderBy(e => e.CreatedAt)
                                                        .ToListAsync();
                }
                else if (eventStream != null)
                {
                    eventStream.Events = await _context.Events.AsNoTracking()
                                                        .Where(e => e.StreamId == eventStream.Id)
                                                        .OrderBy(e => e.CreatedAt)
                                                        .ToListAsync();
                }

                return eventStream;
            }
            catch (Exception ex)
            {
                return null;
            }
            
        }

        public async Task<Entities.EventStream> GetStreamAsync(long streamId, DateTimeOffset? from = null)
        {
            Entities.EventStream eventStream;

            if (from.HasValue)
            {
                eventStream = _context.EventsStream.AsNoTracking().Include(es => es.Events.OrderBy(e => e.CreatedAt)).AsNoTracking()
                                                         .FirstOrDefault(es => es.Id == streamId);

                eventStream.Events = eventStream.Events.Where(e => e.CreatedAt.DateTime > from.Value.DateTime).OrderBy(e => e.CreatedAt).ToList();
            }
            else
            {
                eventStream = _context.EventsStream.AsNoTracking().Include(es => es.Events.OrderBy(e => e.CreatedAt)).AsNoTracking()
                                                         .FirstOrDefault(es => es.Id == streamId);
            }

            return eventStream;

            Thread.Sleep(1);
        }

        public async Task<Entities.EventStream> GetStreamAsync(string streamName, DateTime? from = null)
        {
            streamName = streamName.Trim().ToLower();

            var eventStream = await _context.EventsStream.AsNoTracking()
                                                         .FirstOrDefaultAsync(es => es.Name.ToLower() == streamName);

            if (from.HasValue)
            {
                eventStream.Events = await _context.Events.AsNoTracking()
                                                    .Where(e => (e.StreamId == eventStream.Id) && (e.CreatedAt > from.Value))
                                                    .OrderBy(e => e.CreatedAt)
                                                    .ToListAsync();
            }
            else if (eventStream != null)
            {
                eventStream.Events = await _context.Events.AsNoTracking()
                                                    .Where(e => e.StreamId == eventStream.Id)
                                                    .OrderBy(e => e.CreatedAt)
                                                    .ToListAsync();
            }

            return eventStream;
        }

        public async Task<IEnumerable<ActiveSubscription>> GetActiveSubscriptions()
        {
            return await _context.Subscribers.Include(s => s.Stream)
                                             .Select(s => new ActiveSubscription
                                             {
                                                 StreamId = s.Stream.StreamId,
                                                 SubscriptionId = s.SubscriberId
                                             })
                                             .ToListAsync();
        }

        public async Task<IEnumerable<ActiveAssociatedStreams>> GetAssociatedStreams()
        {
            return await _context.EventStreamsAssociation.AsNoTracking().Include(s => s.Stream)
                                                         .Include(s => s.AssociatedStream)
                                                         .GroupBy(s => s.StreamId, (x, y) => new ActiveAssociatedStreams 
                                                         { 
                                                             StreamId = x, 
                                                             AssociatedStreamIds = y.Select(z => z.AssociatedStreamId)
                                                         })
                                                         .ToListAsync();
        }

        public async Task<EventStreamSubscriber> GetSubscriberAsync(Guid subscriberId, DateTimeOffset? from = null)
        {
            if (from.HasValue)
            {
                var dt = from.Value;

                var subscriber = _context.Subscribers.AsNoTracking().Include(s => s.Stream).Include(s => s.Stream.Events)
                                                        .AsNoTracking().FirstOrDefault(s => s.SubscriberId == subscriberId);

                if (subscriber != null)
                    subscriber.Stream.Events = subscriber.Stream.Events.Where(e => e.CreatedAt.DateTime > dt.DateTime).OrderBy(e => e.CreatedAt).ToList();

                return subscriber;
            }
            else
            {
                var subscriber = _context.Subscribers.Include(s => s.Stream).AsNoTracking().FirstOrDefault(s => s.SubscriberId == subscriberId);

                return subscriber;
            }

            Thread.Sleep(1);
        }
    }
}
