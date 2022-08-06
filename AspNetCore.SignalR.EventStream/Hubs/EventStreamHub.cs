using AspNetCore.SignalR.EventStream.Entities;
using AspNetCore.SignalR.EventStream.HubFilters;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.SignalR.EventStream.Hubs
{
    [EventStreamHubFilter]
    public class EventStreamHub : Hub
    {
        private readonly IRepository _repository;

        public EventStreamHub(IRepository repository)
        {
            _repository = repository;
        }

        public async Task Publish(string streamName, params EventModel[] events)
        {            
            //If stream does not exist in db, create stream and write event to db
            //Else write event to db
            var stream = await _repository.GetStreamAsync(streamName);

            if (stream == null)
            {
                var streamId = Guid.NewGuid();

                await _repository.AddAsync(new Entities.EventStream
                {
                    Name = streamName,                    
                    StreamId = streamId
                });

                var newStream = await _repository.GetStreamAsync(streamId);

                var tmpEvents = new List<Event>();

                foreach(var @event in events)
                {
                    var eventEntity = new Event
                    {
                        Data = @event.Data,
                        MetaData = @event.MetaData,
                        JsonData = @event.JsonData,
                        IsJson = @event.IsJson,
                        Type = @event.Type,             
                        StreamId = newStream.Id
                    };

                    tmpEvents.Add(eventEntity);
                }

                await _repository.AddAsync(tmpEvents.ToArray());
            }
            else
            {
                var tmpEvents = new List<Event>();

                foreach (var @event in events)
                {
                    var eventEntity = new Event
                    {
                        Data = @event.Data,
                        MetaData = @event.MetaData,
                        JsonData = @event.JsonData,
                        IsJson = @event.IsJson,
                        Type = @event.Type,
                        StreamId = stream.Id
                    };

                    tmpEvents.Add(eventEntity);                    
                }

                await _repository.AddAsync(tmpEvents.ToArray());
            }
        }

        public async Task Subscribe(string streamName, string type, string receiveMethod, Guid subscriberId, Guid subscribeKey, DateTime? from = null)
        {
            var stream = await _repository.GetStreamAsync(streamName);

            if (stream != null)
            {
                //Create entry in stream subscriber table in db

                await _repository.AddAsync(new EventStreamSubscriber
                {
                    ConnectionId = this.Context.ConnectionId,
                    ReceiveMethod = receiveMethod,                    
                    SubscriberId = subscriberId,
                    SubscribeKey = subscribeKey,
                    StreamId = stream.Id
                });
            }
            else
            {
                var streamId = Guid.NewGuid();

                await _repository.AddAsync(new Entities.EventStream
                {
                    Name = streamName,
                    StreamId = streamId
                });

                var newStream = await _repository.GetStreamAsync(streamId);

                await _repository.AddAsync(new EventStreamSubscriber
                {
                    ConnectionId = this.Context.ConnectionId,
                    ReceiveMethod = receiveMethod,
                    SubscriberId = subscriberId,
                    SubscribeKey = subscribeKey,
                    StreamId = newStream.Id
                });
            }
        }

        public async Task Unsubscribe(string streamName, Guid subscriberId, Guid subscribeKey)
        {
            var stream = await _repository.GetStreamAsync(streamName);

            var subscriber = await _repository.GetSubscriberAsync(subscriberId, stream.StreamId);

            if ((subscriber == null) || (subscriber.SubscribeKey != subscribeKey))
            {
                throw new ApplicationException("Subscriber not found. Please subscribe first, to then unsubscribe.");
            }

            if (stream != null)
            {
                //Remove entry from stream subscriber table in db

                await _repository.DeleteSubscriptionAsync(subscriberId, stream.StreamId);
            }            
        }

        public async Task EventStreamEventAppeared(EventStreamSubscriberModel subscriber)
        {
            try
            {
                var eventsArrayJson = System.Text.Json.JsonSerializer.Serialize(subscriber.Stream.Events);

                await base.Clients.Client(subscriber.ConnectionId).SendAsync(subscriber.ReceiveMethod, eventsArrayJson, new object());
            }
            catch (Exception ex)
            {
                //Log exception
            }
        }
    }
}
