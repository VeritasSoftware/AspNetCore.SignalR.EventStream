﻿using AspNetCore.SignalR.EventStream.Entities;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream.Repositories
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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Events.Add(@event);

                await _context.SaveChangesAsync();

                var stream = await _context.EventsStream.FirstOrDefaultAsync(s => s.Id == @event.StreamId);
                if (stream != null)
                {
                    stream.LastEventInsertedAt = DateTimeOffset.UtcNow;

                    _context.Update(stream);

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
            }            
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

        public async Task AddAsync(EventStreamAssociation association)
        {
            try
            {
                _context.EventStreamsAssociation.Add(association);
                await _context.SaveChangesAsync();
            }
            catch(Exception ex)
            {

            }            
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
            var events =  await _context.EventsStream.Include(es => es.Events)
                                                     .FirstOrDefaultAsync(es => es.StreamId == streamId);

            if (from.HasValue)
                events.Events = events.Events.Where(e => e.CreatedAt > from.Value)
                                             .OrderBy(e => e.CreatedAt)
                                             .ToList();

            return events;
        }

        public async Task<Entities.EventStream> GetStreamAsync(long streamId, DateTimeOffset? from = null)
        {
            var eventStream = await _context.EventsStream.Include(es => es.Events)
                                                         .FirstOrDefaultAsync(es => es.Id == streamId);

            if (from.HasValue)
                eventStream.Events = eventStream.Events.Where(e => e.CreatedAt <= from.Value)
                                                       .OrderBy(e => e.CreatedAt)
                                                       .ToList();

            return eventStream;
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

        public async Task<IEnumerable<ActiveAssociatedStreams>> GetAssociatedStreams()
        {
            return await _context.EventStreamsAssociation.Include(s => s.Stream)
                                                         .Include(s => s.AssociatedStream)
                                                         .GroupBy(s => s.StreamId, (x, y) => new ActiveAssociatedStreams 
                                                         { 
                                                             StreamId = x, 
                                                             AssociatedStreamIds = y.Select(z => z.AssociatedStreamId)
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