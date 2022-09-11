using AspNetCore.SignalR.EventStream.Entities;
using AspNetCore.SignalR.EventStream.HubFilters;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.SignalR.EventStream.Hubs
{
    [EventStreamHubFilter]
    [Authorize(Policy = "EventStreamHubPolicy")]
    public class EventStreamHub : Hub
    {
        private readonly IRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventStreamHub>? _logger;

        public EventStreamHub(IRepository repository, IConfiguration configuration, ILogger<EventStreamHub>? logger = null)
        {
            _repository = repository;
            _configuration = configuration;
            _logger = logger;
        }

        [Authorize(Policy = "EventStreamHubPublishPolicy")]
        public async Task Publish(string streamName, params EventModel[] events)
        {
            try
            {
                _logger?.LogInformation($"Publishing to stream {streamName}, {events.Length} events. ConnectionId: {this.Context.ConnectionId}.");
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

                    foreach (var @event in events)
                    {
                        var eventEntity = new Event
                        {
                            Data = @event.Data,
                            Base64StringData = @event.Base64StringData,
                            MetaData = @event.MetaData,
                            Base64StringMetaData = @event.Base64StringMetaData,
                            JsonData = @event.JsonData,
                            IsJson = @event.IsJson,
                            IsBase64String = @event.IsBase64String,
                            Type = @event.Type,
                            Description = @event.Description,
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
                            Base64StringData = @event.Base64StringData,
                            MetaData = @event.MetaData,
                            Base64StringMetaData = @event.Base64StringMetaData,
                            JsonData = @event.JsonData,
                            IsJson = @event.IsJson,
                            IsBase64String = @event.IsBase64String,
                            Type = @event.Type,
                            Description = @event.Description,
                            StreamId = stream.Id
                        };

                        tmpEvents.Add(eventEntity);
                    }

                    await _repository.AddAsync(tmpEvents.ToArray());
                }

                _logger?.LogInformation($"Finished publishing to stream {streamName}, {events.Length} events. ConnectionId: {this.Context.ConnectionId}.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamHub)} error. Stream Name: {streamName}, ConnectionId: {Context.ConnectionId}.");
                throw;
            }
        }

        [Authorize(Policy = "EventStreamHubSubscribePolicy")]
        public async Task Subscribe(SubscribeModel model)
        {
            try
            {
                _logger?.LogInformation($"Subscribing to stream {model.StreamName}. ConnectionId: {this.Context.ConnectionId}. SubscriberId: {model.SubscriberId}.");

                var stream = await _repository.GetStreamAsync(model.StreamName);

                if (stream != null)
                {
                    //Create entry in stream subscriber table in db

                    await _repository.AddAsync(new EventStreamSubscriber
                    {
                        ConnectionId = this.Context.ConnectionId,
                        SubscriberId = model.SubscriberId,
                        SubscriberKey = model.SubscriberKey,
                        StreamId = stream.Id,
                        LastAccessedFromEventId = 0
                    }); ;
                }
                else
                {
                    var streamId = Guid.NewGuid();

                    await _repository.AddAsync(new Entities.EventStream
                    {
                        Name = model.StreamName,
                        StreamId = streamId
                    });

                    var newStream = await _repository.GetStreamAsync(streamId);

                    await _repository.AddAsync(new EventStreamSubscriber
                    {
                        ConnectionId = this.Context.ConnectionId,
                        SubscriberId = model.SubscriberId,
                        SubscriberKey = model.SubscriberKey,
                        StreamId = newStream.Id,
                        LastAccessedFromEventId = 0
                    });
                }

                _logger?.LogInformation($"Finished subscribing to stream {model.StreamName}. ConnectionId: {this.Context.ConnectionId}. SubscriberId: {model.SubscriberId}.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamHub)} error. SubscriberId: {model.SubscriberId}, ConnectionId: {Context.ConnectionId}.");
                throw;
            }            
        }

        [Authorize(Policy = "EventStreamHubUnsubscribePolicy")]
        public async Task Unsubscribe(string streamName, Guid subscriberId, Guid subscribeKey)
        {
            try
            {
                _logger?.LogInformation($"Unsubscribing from stream {streamName}. ConnectionId: {this.Context.ConnectionId}. SubscriberId: {subscriberId}.");

                var stream = await _repository.GetStreamAsync(streamName);

                var subscriber = await _repository.GetSubscriberAsync(subscriberId);

                if ((subscriber == null) || (subscriber.SubscriberKey != subscribeKey))
                {
                    throw new ApplicationException("Subscriber not found. Please subscribe first, to then unsubscribe.");
                }

                if (stream != null)
                {
                    //Remove entry from stream subscriber table in db

                    await _repository.DeleteSubscriptionAsync(subscriberId, stream.StreamId);
                }

                _logger?.LogInformation($"Finished unsubscribing from stream {streamName}. ConnectionId: {this.Context.ConnectionId}. SubscriberId: {subscriberId}.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{nameof(EventStreamHub)} error. SubscriberId: {subscriberId}, ConnectionId: {Context.ConnectionId}.");
                throw;
            }
        }

        public async Task EventStreamEventAppeared(EventStreamSubscriberModelResult subscriber, string secretKey)
        {
            try
            {
                if (string.IsNullOrEmpty(secretKey))
                    throw new ArgumentNullException(nameof(secretKey));

                var configSecretKey = _configuration["EventStreamSecretKey"];

                if (string.IsNullOrEmpty(configSecretKey))
                    throw new ArgumentNullException(nameof(configSecretKey));

                if (string.Compare(secretKey, configSecretKey, StringComparison.Ordinal) == 0)
                {
                    var eventsArrayJson = System.Text.Json.JsonSerializer.Serialize(subscriber.Events);

                    _logger?.LogInformation($"Sending events ({subscriber.Events.Count()}) to subscribers ({subscriber.ConnectionIds.Count}) of stream {subscriber.StreamName}. ConnectionId: {this.Context.ConnectionId}.");

                    await base.Clients.Clients(subscriber.ConnectionIds).SendAsync(subscriber.StreamName, eventsArrayJson, new object());

                    _logger?.LogInformation($"Finished sending events ({subscriber.Events.Count()}) to subscribers ({subscriber.ConnectionIds.Count}) of stream {subscriber.StreamName}. ConnectionId: {this.Context.ConnectionId}.");
                }                
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error sending events ({subscriber.Events.Count()}) to subscribers ({subscriber.ConnectionIds.Count}) of stream {subscriber.StreamName}. ConnectionId: {this.Context.ConnectionId}.");
            }
        }
    }
}
