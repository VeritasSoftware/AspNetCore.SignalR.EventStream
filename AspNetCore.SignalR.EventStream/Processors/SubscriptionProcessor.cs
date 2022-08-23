using AspNetCore.SignalR.EventStream.Clients;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.AspNetCore.SignalR.Client;

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
        
        private string Name => typeof(SubscriptionProcessor).Name;

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
                    if ((_eventStreamHubClient.HubConnection == null) || (_eventStreamHubClient.HubConnection.State != HubConnectionState.Connected))
                    {
                        await _eventStreamHubClient.StartAsync();
                    }

                    if (_eventStreamHubClient.HubConnection.State == HubConnectionState.Connected)
                    {
                        var activeSubscriptions = await _repository.GetActiveSubscriptions();

                        foreach (var subscription in activeSubscriptions)
                        {
                            try
                            {
                                var subscriber = await _repository.GetSubscriberAsync(subscription.SubscriptionId);

                                if (subscriber == null)
                                {
                                    continue;
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

                                        _logger?.LogInformation($"Finished streaming events ({subsciptionWithEvents.Stream.Events.Count()}) to subscriber {subscriber.SubscriberId}.");                                     
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

            try
            {
                _logger?.LogInformation($"Stopping { Name } thread.");
                _processorThread.Abort();
                _logger?.LogInformation($"Finished { Name } stopping thread.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error stopping { Name } thread.");
            }
        }
    }
}
