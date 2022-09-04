using AspNetCore.SignalR.EventStream.Clients;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public class SubscriptionProcessor : IAsyncDisposable
    {
        private readonly IRepository _repository;
        private readonly IEventStreamHubClient _eventStreamHubClient;
        private readonly ISubscriptionProcessorNotifier _notifier;
        private readonly ILogger<SubscriptionProcessor>? _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _start = false;

        public bool Start
        {
            get
            {                
                return _start;
            }
            set
            {
                if (!value)
                {
                    _logger?.LogInformation("Detaching On Events Added Notifier.");
                    _notifier.OnEventsAdded -= OnEventsAddedHandler;
                    _logger?.LogInformation("Finished detaching On Events Added Notifier.");
                    _logger.LogInformation($"{Name} stopped.");
                }
                else
                {
                    _logger?.LogInformation("Attaching On Events Added Notifier.");
                    _notifier.OnEventsAdded += OnEventsAddedHandler;
                    _logger?.LogInformation("Finished attaching On Events Added Notifier.");
                    _logger.LogInformation($"{Name} started.");
                }                

                _start = value;
            }
        }
        public string? EventStreamHubUrl { get; set; }
        public string? SecretKey { get; set;}        

        public string Name => nameof(SubscriptionProcessor);

        public SubscriptionProcessor(IServiceProvider serviceProvider, IEventStreamHubClient eventStreamHubClient, 
                            ISubscriptionProcessorNotifier notifier, 
                            ILogger<SubscriptionProcessor>? logger = null)
        {
            _repository = serviceProvider.GetRequiredService<IRepository>();
            _serviceProvider = serviceProvider;
            _eventStreamHubClient = eventStreamHubClient;
            _notifier = notifier;
            _logger = logger;
        }

        private async Task OnEventsAddedHandler(IEnumerable<Entities.Event> events)
        {
            try
            {
                if (!_eventStreamHubClient.IsConnected)
                {
                    await _eventStreamHubClient.StartAsync();
                }

                if (_eventStreamHubClient.IsConnected)
                {
                    var streamIdInt = events.First().StreamId;

                    var stream = await _repository.GetStreamAsync(streamIdInt);

                    if (stream == null)
                        throw new InvalidOperationException($"Stream {streamIdInt} not found.");

                    var streamId = stream.StreamId;
                    var streamName = stream.Name;

                    var activeSubscriptions = await _repository.GetActiveSubscriptionsAsync(streamIdInt);

                    if (activeSubscriptions.Any())
                    {
                        _logger?.LogInformation($"Streaming events ({events.Count()}) to subscribers ({activeSubscriptions.Count()}) of stream {streamName}.");

                        var result = new EventStreamSubscriberModelResult
                        {
                            Events = events.Select(x => new EventModelResult
                            {
                                Data = x.Data,
                                Base64StringData = x.Base64StringData,
                                Id = x.Id,
                                EventId = x.EventId,
                                IsJson = x.IsJson,
                                IsBase64String = x.IsBase64String,
                                JsonData = x.JsonData,
                                MetaData = x.MetaData,
                                Base64StringMetaData = x.Base64StringMetaData,
                                StreamId = streamId,
                                StreamName = streamName,
                                OriginalEventId = x.OriginalEventId,
                                Type = x.Type,
                                Description = x.Description,
                                CreatedAt = x.CreatedAt
                            }).ToList(),
                            ConnectionIds = activeSubscriptions.Select(x => x.ConnectionId).ToList(),
                            StreamName = streamName
                        };                        

                        await _eventStreamHubClient.SendAsync(result);

                        _logger?.Log(
                                    LogLevel.Information,
                                    1000,
                                    $"Finished streaming events ({events.Count()}) to subscribers ({activeSubscriptions.Count()}) of stream {streamName}.",
                                    null,
                                    null);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in {Name} thread.");
            }
        }

        public async Task ProcessSubscriber(Guid subscriberId)
        {
            var repository = _serviceProvider.GetRequiredService<IRepository>();

            var subscriber = await repository.GetSubscriberAsync(subscriberId);

            if (subscriber == null)
            {
                _logger?.LogWarning($"Subscriber {subscriberId} not found.");
                return;
            }

            var subsciptionWithEvents = await repository.GetSubscriberAsync(subscriberId,
                                                            subscriber.LastAccessedCurrentEventId > subscriber.LastAccessedFromEventId ?
                                                                subscriber.LastAccessedCurrentEventId : subscriber.LastAccessedFromEventId, subscriber.LastAccessedToEventId);

            if (subsciptionWithEvents != null)
            {
                if (subsciptionWithEvents.Stream.Events != null && subsciptionWithEvents.Stream.Events.Any())
                {
                    _logger?.LogInformation($"Streaming events ({subsciptionWithEvents.Stream.Events.Count()}) to subscriber {subscriber.SubscriberId}.");

                    var eventSubscriberModel = new EventStreamSubscriberModelResult
                    {
                        ConnectionIds = new List<string> { subsciptionWithEvents.ConnectionId },
                        Events = subsciptionWithEvents.Stream.Events.Select(x => new EventModelResult
                        {
                            Data = x.Data,
                            Base64StringData = x.Base64StringData,
                            Id = x.Id,
                            EventId = x.EventId,
                            IsJson = x.IsJson,                            
                            IsBase64String = x.IsBase64String,
                            JsonData = x.JsonData,
                            MetaData = x.MetaData,
                            Base64StringMetaData = x.Base64StringMetaData,
                            StreamId = subsciptionWithEvents.Stream.StreamId,
                            StreamName = subsciptionWithEvents.Stream.Name,
                            OriginalEventId = x.OriginalEventId,
                            Type = x.Type,
                            Description = x.Description,
                            CreatedAt = x.CreatedAt
                        }).ToList(),
                        StreamName  = subsciptionWithEvents.Stream.Name
                    };

                    await _eventStreamHubClient.SendAsync(eventSubscriberModel);

                    _logger?.Log(
                        LogLevel.Information,
                        1000,
                        $"Finished streaming events ({subsciptionWithEvents.Stream.Events.Count()}) to subscriber {subscriber.SubscriberId}.",
                        null,
                        null);

                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}
