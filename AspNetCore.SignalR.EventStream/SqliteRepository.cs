using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream
{
    public class SqliteRepository : IRepository
    {
        private readonly SqliteDbContext _context;

        public SqliteRepository(SqliteDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Event @event)
        {
            _context.Events.Add(@event);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(Entities.EventStream eventStream)
        {
            _context.EventsStream.Add(eventStream);
            await _context.SaveChangesAsync();
        }

        public async Task AddAsync(EventStreamSubscriber subscriber)
        {
            _context.Subscribers.Add(subscriber);
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

        public async Task<Entities.EventStream> GetStreamAsync(Guid streamId, DateTime? from = null)
        {
            var events =  await _context.EventsStream.Include(es => es.Events)
                                                     .FirstOrDefaultAsync(es => es.StreamId == streamId);

            if (from.HasValue)
                events.Events = events.Events.Where(e => e.CreatedAt > from.Value)
                                             .OrderBy(e => e.CreatedAt)
                                             .ToList();

            return events;
        }

        public async Task<Entities.EventStream> GetStreamAsync(string streamName, DateTime? from = null)
        {
            streamName = streamName.Trim().ToLower();

            var events = await _context.EventsStream.Include(es => es.Events)
                                                     .FirstOrDefaultAsync(es => es.Name.ToLower() == streamName);

            if (from.HasValue)
                events.Events = events.Events.Where(e => e.CreatedAt > from.Value)
                                             .OrderBy(e => e.CreatedAt)
                                             .ToList();

            return events;
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

        public async Task<EventStreamSubscriber> GetSubscriberAsync(Guid subscriberId, Guid streamId, DateTime? from = null)
        {
            var subscriber = await _context.Subscribers.Include(s => s.Stream)
                                                       .Include(s => s.Stream.Events)
                                                       .FirstOrDefaultAsync(s => (s.Stream.StreamId == streamId) && (s.SubscriberId == subscriberId));

            if (subscriber == null)
            {
                return null;
            }

            if (from.HasValue)
                subscriber.Stream.Events = subscriber.Stream.Events.Where(e => e.CreatedAt > from.Value)
                                                                   .OrderBy(e => e.CreatedAt)
                                                                   .ToList();
            else if (subscriber.LastAccessedEventAt.HasValue)
                subscriber.Stream.Events = subscriber.Stream.Events.Where(e => e.CreatedAt > subscriber.LastAccessedEventAt.Value)
                                                                   .OrderBy(e => e.CreatedAt)
                                                                   .ToList();

            return subscriber;
        }
    }
}
