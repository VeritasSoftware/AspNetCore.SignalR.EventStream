using AspNetCore.SignalR.EventStream.Clients;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public class SubscriptionProcessor : IAsyncDisposable
    {
        private readonly IRepository _repository;
        private static Thread _processorThread = null;
        private readonly IEventStreamHubClient _eventStreamHubClient;
        private readonly ILogger<SubscriptionProcessor>? _logger;

        public bool Start { get; set; } = false;
        public string? EventStreamHubUrl { get; set; }
        public string? SecretKey { get; set;}
        public int MaxDegreeOfParallelism { get; set; }


        private string Name => nameof(SubscriptionProcessor);

        public SubscriptionProcessor(IRepository repository, IEventStreamHubClient eventStreamHubClient, ILogger<SubscriptionProcessor>? logger = null)
        {
            _repository = repository;
            _eventStreamHubClient = eventStreamHubClient;
            _logger = logger;
        }

        public void Process()
        {
            _processorThread = new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                /* run your code here */
                _logger?.LogInformation($"Starting {Name} process.");

                await this.ProcessAsync();
            });

            _logger?.LogInformation($"Starting {Name} thread.");
            _processorThread.Start();
        }

        private async Task ProcessAsync()
        {           
            while (Start)
            {
                try
                {
                    if (!_eventStreamHubClient.IsConnected)
                    {
                        await _eventStreamHubClient.StartAsync();
                    }

                    if (_eventStreamHubClient.IsConnected)
                    {
                        var activeSubscriptions = await _repository.GetActiveSubscriptionsAsync();

                        if (activeSubscriptions.Any())
                        {
                            Parallel.ForEach(activeSubscriptions, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, async (subscription) =>
                            {
                                try
                                {
                                    var subscriber = await _repository.GetSubscriberAsync(subscription.SubscriptionId);

                                    if (subscriber == null)
                                    {
                                        return;
                                    }

                                    //_logger?.LogInformation($"LastAccessedEventId: {subscriber.LastAccessedEventId}.");

                                    var subsciptionWithEvents = await _repository.GetSubscriberAsync(subscription.SubscriptionId,
                                                                                    subscriber.LastAccessedCurrentEventId > subscriber.LastAccessedFromEventId ?
                                                                                        subscriber.LastAccessedCurrentEventId : subscriber.LastAccessedFromEventId, subscriber.LastAccessedToEventId);

                                    if (subsciptionWithEvents != null)
                                    {
                                        if (subsciptionWithEvents.Stream.Events != null && subsciptionWithEvents.Stream.Events.Any())
                                        {
                                            _logger?.LogInformation($"Streaming events ({subsciptionWithEvents.Stream.Events.Count()}) to subscriber {subscriber.SubscriberId}.");

                                            var eventSubscriberModel = new EventStreamSubscriberModelResult
                                            {
                                                ConnectionId = subsciptionWithEvents.ConnectionId,
                                                CreatedAt = subsciptionWithEvents.CreatedAt,
                                                ReceiveMethod = subsciptionWithEvents.ReceiveMethod,
                                                StreamId = subsciptionWithEvents.StreamId,
                                                SubscriberId = subsciptionWithEvents.SubscriberId,
                                                LastAccessedEventId = subsciptionWithEvents.LastAccessedFromEventId,
                                                Stream = new EventStreamModelResult
                                                {
                                                    Name = subsciptionWithEvents.Stream.Name,
                                                    Events = subsciptionWithEvents.Stream.Events.Select(x => new EventModelResult
                                                    {
                                                        Data = x.Data,
                                                        Id = x.Id,
                                                        EventId = x.EventId,
                                                        IsJson = x.IsJson,
                                                        JsonData = x.JsonData,
                                                        MetaData = x.MetaData,
                                                        StreamId = subsciptionWithEvents.Stream.StreamId,
                                                        StreamName = subsciptionWithEvents.Stream.Name,
                                                        OriginalEventId = x.OriginalEventId,
                                                        Type = x.Type,
                                                        CreatedAt = x.CreatedAt
                                                    }).ToList(),
                                                    CreatedAt = subsciptionWithEvents.Stream.CreatedAt,
                                                    StreamId = subsciptionWithEvents.Stream.StreamId
                                                }
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
                                catch (Exception ex)
                                {
                                    _logger?.LogError(ex, $"Error in {Name} thread.");
                                    return;
                                }
                            });
                        }                        
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"Error in {Name} thread.");
                    continue;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Start = false;

            await Task.CompletedTask;
        }
    }

    public class SubscriptionProcessorState
    {
        public string Message { get; set; }
    }

}
