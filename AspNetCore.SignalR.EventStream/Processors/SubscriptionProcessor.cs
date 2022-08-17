using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.AspNetCore.SignalR.Client;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public class SubscriptionProcessor : IAsyncDisposable
    {
        private readonly IRepository _repository;
        private static Thread _processorThread = null;
        HubConnection _hubConnection = null;
        private readonly ILogger<SubscriptionProcessor>? _logger;

        public bool Start { get; set; } = false;
        public string? EventStreamHubUrl { get; set; }
        public string? SecretKey { get; set;}

        public SubscriptionProcessor(IRepository repository, string eventStreamHubUrl, string secretKey, ILogger<SubscriptionProcessor>? logger = null)
        {
            _repository = repository;
            this.EventStreamHubUrl = eventStreamHubUrl;
            SecretKey = secretKey;
            _logger = logger;
        }

        public void Process()
        {
            _processorThread = new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                /* run your code here */
                _logger?.LogInformation("Starting SubscriptionProcessor process.");

                await this.ProcessAsync();
            });

            _logger?.LogInformation("Starting SubscriptionProcessor thread.");
            _processorThread.Start();
        }

        private async Task ProcessAsync()
        {           
            while (Start)
            {
                try
                {
                    if ((_hubConnection == null) || (_hubConnection.State != HubConnectionState.Connected))
                    {
                        try
                        {
                            _logger?.LogInformation($"Starting event stream hub connection. Hub url: {this.EventStreamHubUrl}.");

                            _hubConnection = new HubConnectionBuilder()
                            .WithUrl(this.EventStreamHubUrl)
                            .WithAutomaticReconnect()
                            .AddNewtonsoftJsonProtocol()
                            .Build();

                            await _hubConnection.StartAsync();

                            _logger?.LogInformation($"Finished starting event stream hub connection. Hub url: {this.EventStreamHubUrl}.");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, $"Error starting event stream hub connection. Hub url: {this.EventStreamHubUrl}.");
                            continue;
                        }
                    }

                    if (_hubConnection.State == HubConnectionState.Connected)
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

                                var subsciptionWithEvents = await _repository.GetSubscriberAsync(subscription.SubscriptionId, subscriber.LastAccessedEventId);

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
                                            LastAccessedEventId = subsciptionWithEvents.LastAccessedEventId,
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

                                        await _hubConnection.InvokeAsync("EventStreamEventAppeared", eventSubscriberModel, SecretKey);

                                        _logger?.LogInformation($"Finished streaming events ({subsciptionWithEvents.Stream.Events.Count()}) to subscriber {subscriber.SubscriberId}.");

                                        var id = subsciptionWithEvents.Stream.Events.Last().Id;

                                        await _repository.UpdateSubscriptionLastAccessedAsync(subsciptionWithEvents.SubscriberId, id);                                        
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, "Error in SubscriptionProcessor thread.");
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in SubscriptionProcessor thread.");
                    continue;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Start = false;

            try
            {
                _logger?.LogInformation($"Stopping event stream hub connection. Hub url: {this.EventStreamHubUrl}.");

                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
                _logger?.LogInformation($"Finished stopping event stream hub connection. Hub url: {this.EventStreamHubUrl}.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error stopping event stream hub connection. Hub url: {this.EventStreamHubUrl}.");
            }

            try
            {
                _logger?.LogInformation("Stopping thread.");
                _processorThread.Abort();
                _logger?.LogInformation("Finished stopping thread.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping thread.");
            }
        }
    }
}
