using AspNetCore.SignalR.EventStream.Clients;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public class SubscriptionProcessor : IAsyncDisposable
    {
        private readonly IRepository _repository;
        private static Thread? _processorThread = null;
        private readonly IEventStreamHubClient _eventStreamHubClient;
        private readonly ISubscriptionProcessorEventHandler _subscriptionProcessorEventHandler;
        private readonly ILogger<SubscriptionProcessor>? _logger;

        private CancellationTokenSource? _cancellationTokenSource;
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
                    _cancellationTokenSource?.Cancel();
                    _logger?.LogInformation("Detaching On Events Added Handler");
                    _subscriptionProcessorEventHandler.OnEventsAdded -= OnEventsAddedHandler;
                    _logger?.LogInformation("Finished detaching On Events Added Handler");
                    _logger.LogInformation($"{Name} stopped.");
                }
                else
                {
                    _logger?.LogInformation("Attaching On Events Added Handler");
                    _subscriptionProcessorEventHandler.OnEventsAdded += OnEventsAddedHandler;
                    _logger?.LogInformation("Finished attaching On Events Added Handler");
                    _logger.LogInformation($"{Name} started.");
                }                

                _start = value;
            }
        }
        public string? EventStreamHubUrl { get; set; }
        public string? SecretKey { get; set;}
        public int MaxDegreeOfParallelism { get; set; }

        private readonly IServiceProvider _serviceProvider;

        public string Name => nameof(SubscriptionProcessor);

        public SubscriptionProcessor(IServiceProvider serviceProvider, IEventStreamHubClient eventStreamHubClient, 
                            ISubscriptionProcessorEventHandler subscriptionProcessorEventHandler, 
                            ILogger<SubscriptionProcessor>? logger = null)
        {
            _repository = serviceProvider.GetRequiredService<IRepository>();
            _serviceProvider = serviceProvider;
            _eventStreamHubClient = eventStreamHubClient;
            _subscriptionProcessorEventHandler = subscriptionProcessorEventHandler;
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
                    var streamId = events.First().StreamId;

                    var activeSubscriptions = await _repository.GetActiveSubscriptionsAsync(streamId);

                    if (activeSubscriptions.Any())
                    {
                        using (_cancellationTokenSource = new CancellationTokenSource())
                        {
                            Parallel.ForEach(activeSubscriptions, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = _cancellationTokenSource.Token },
                            async (subscription) =>
                            {
                                try
                                {
                                    var repository = _serviceProvider.GetRequiredService<IRepository>();

                                    _logger?.LogInformation($"Streaming events ({events.Count()}) to subscriber {subscription.SubscriberId}.");

                                    var eventSubscriberModel = new EventStreamSubscriberModelResult
                                    {
                                        ConnectionId = subscription.ConnectionId,
                                        CreatedAt = subscription.CreatedAt,
                                        ReceiveMethod = subscription.ReceiveMethod,
                                        StreamId = subscription.StreamId,
                                        SubscriberId = subscription.SubscriberId,
                                        LastAccessedEventId = subscription.LastAccessedFromEventId,
                                        Stream = new EventStreamModelResult
                                        {
                                            Name = subscription.Stream.Name,
                                            Events = events.Select(x => new EventModelResult
                                            {
                                                Data = x.Data,
                                                Id = x.Id,
                                                EventId = x.EventId,
                                                IsJson = x.IsJson,
                                                JsonData = x.JsonData,
                                                MetaData = x.MetaData,
                                                StreamId = subscription.Stream.StreamId,
                                                StreamName = subscription.Stream.Name,
                                                OriginalEventId = x.OriginalEventId,
                                                Type = x.Type,
                                                CreatedAt = x.CreatedAt
                                            }).ToList(),
                                            CreatedAt = subscription.Stream.CreatedAt,
                                            StreamId = subscription.Stream.StreamId
                                        }
                                    };

                                    await _eventStreamHubClient.SendAsync(eventSubscriberModel);

                                    _logger?.Log(
                                        LogLevel.Information,
                                        1000,
                                        $"Finished streaming events ({events.Count()}) to subscriber {subscription.SubscriberId}.",
                                        null,
                                        null);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogError(ex, $"Error in {Name} thread.");
                                    return;
                                }
                            });
                        }

                        _cancellationTokenSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in {Name} thread.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Task.CompletedTask;
        }
    }
}
