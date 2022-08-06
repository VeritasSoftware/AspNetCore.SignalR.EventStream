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

        public bool Start { get; set; } = false;
        public string? EventStreamHubUrl { get; set; }

        public SubscriptionProcessor(IRepository repository, string eventStreamHubUrl)
        {
            _repository = repository;
            this.EventStreamHubUrl = eventStreamHubUrl;
        }

        public void Process()
        {
            _processorThread = new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                /* run your code here */
                await this.ProcessAsync();
            });

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
                            _hubConnection = new HubConnectionBuilder()
                            .WithUrl(this.EventStreamHubUrl)
                            .WithAutomaticReconnect()
                            .AddNewtonsoftJsonProtocol()
                            .Build();

                            await _hubConnection.StartAsync();
                        }
                        catch (Exception ex)
                        {
                            //TODO: Log exception
                        }
                    }

                    if (_hubConnection.State == HubConnectionState.Connected)
                    {
                        var activeSubscriptions = await _repository.GetActiveSubscriptions();

                        foreach (var subscription in activeSubscriptions)
                        {
                            try
                            {
                                var subscriber = await _repository.GetSubscriberAsync(subscription.SubscriptionId, subscription.StreamId);

                                var subsciptionWithEvents = await _repository.GetSubscriberAsync(subscription.SubscriptionId, subscription.StreamId, subscriber.LastAccessedEventAt);

                                if (subsciptionWithEvents != null)
                                {
                                    if (subsciptionWithEvents.Stream.Events != null && subsciptionWithEvents.Stream.Events.Any())
                                    {
                                        //TODO:Update subscriber Last Accessed                                    

                                        var eventSubscriberModel = new EventStreamSubscriberModel
                                        {
                                            ConnectionId = subsciptionWithEvents.ConnectionId,
                                            CreatedAt = subsciptionWithEvents.CreatedAt,
                                            ReceiveMethod = subsciptionWithEvents.ReceiveMethod,
                                            StreamId = subsciptionWithEvents.StreamId,
                                            SubscriberId = subsciptionWithEvents.SubscriberId,
                                            LastAccessedEventAt = subsciptionWithEvents.LastAccessedEventAt,
                                            Stream = new EventStreamModel
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
                                                    StreamId = x.Stream.StreamId,
                                                    StreamName = x.Stream.Name,
                                                    OriginalEventId = x.OriginalEventId,
                                                    Type = x.Type
                                                }).ToList(),
                                                CreatedAt = subsciptionWithEvents.Stream.CreatedAt,
                                                StreamId = subsciptionWithEvents.Stream.StreamId
                                            }
                                        };

                                        await _hubConnection.InvokeAsync("EventStreamEventAppeared", eventSubscriberModel);

                                        await _repository.UpdateSubscriptionLastAccessed(subsciptionWithEvents.SubscriberId, DateTimeOffset.UtcNow);                                        
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //TODO: Log exception
                                continue;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //TODO: Logging
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Start = false;

            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }
            catch (Exception)
            {
                //TODO: Logging
            }

            try
            {
                _processorThread.Abort();
            }
            catch (Exception)
            {
                //TODO: Logging
            }
        }
    }
}
