﻿using AspNetCore.SignalR.EventStream.Entities;
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
            lock(_lock)
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _context.Events.AddRange(@event);

                        _context.SaveChanges();

                        var stream = _context.EventsStream.FirstOrDefault(s => s.Id == @event.First().StreamId);
                        if (stream != null)
                        {
                            stream.LastEventInsertedAt = DateTime.UtcNow;

                            _context.EventsStream.Update(stream);

                            _context.SaveChanges();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
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

        public async Task UpdateSubscriptionLastAccessed(Guid subsciberId, DateTimeOffset lastAccessed)
        {
            var subscriber = await _context.Subscribers.FirstOrDefaultAsync(s => s.SubscriberId == subsciberId);
            subscriber.LastAccessedEventAt = lastAccessed;
            _context.Subscribers.Update(subscriber);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubscriptionAsync(Guid subscriptionId, Guid streamId)
        {
            var subscription = await _context.Subscribers.Include(s => s.Stream)
                                             .FirstOrDefaultAsync(s => (s.Stream.StreamId == streamId) && (s.SubscriberId == subscriptionId));

            _context.Remove(subscription);
            await _context.SaveChangesAsync();
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
            lock(_lock)
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
            }

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

        public async Task<EventStreamSubscriber> GetSubscriberAsync(Guid subscriberId, Guid streamId, DateTimeOffset? from = null)
        {
            lock(_lock)
            {
                if (from.HasValue)
                {
                    var dt = from.Value;

                    var subscriber = _context.Subscribers.AsNoTracking().Include(s => s.Stream).Include(s => s.Stream.Events)
                                                            .AsNoTracking().FirstOrDefault(s => (s.Stream.StreamId == streamId) && (s.SubscriberId == subscriberId));

                    subscriber.Stream.Events = subscriber.Stream.Events.Where(e => e.CreatedAt.DateTime > dt.DateTime).OrderBy(e => e.CreatedAt).ToList();

                    return subscriber;
                }
                else
                {
                    var subscriber = _context.Subscribers.AsNoTracking().FirstOrDefault(s => (s.Stream.StreamId == streamId) && (s.SubscriberId == subscriberId));

                    return subscriber;
                }
            }

            Thread.Sleep(1);
        }
    }
}
