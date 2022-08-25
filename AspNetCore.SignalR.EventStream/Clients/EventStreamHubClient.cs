using AspNetCore.SignalR.EventStream.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace AspNetCore.SignalR.EventStream.Clients
{
    internal class EventStreamHubClient : IEventStreamHubClient, IAsyncDisposable
    {
        HubConnection? _hubConnection = null;
        private readonly ILogger<EventStreamHubClient>? _logger;

        public string EventStreamHubUrl { get; set; }
        public string SecretKey { get; set; }
        public virtual bool IsConnected => _hubConnection != null && _hubConnection.State == HubConnectionState.Connected;

        public EventStreamHubClient(string eventStreamHubUrl, string secretKey, ILogger<EventStreamHubClient>? logger = null)
        {
            this.EventStreamHubUrl = eventStreamHubUrl;
            this.SecretKey = secretKey;
            _logger = logger;
        }

        public async Task SendAsync(EventStreamSubscriberModelResult modelResult)
        {
            await _hubConnection.InvokeAsync("EventStreamEventAppeared", modelResult, SecretKey);
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger?.LogInformation($"Starting event stream hub connection. Hub url: {this.EventStreamHubUrl}.");

                _hubConnection = new HubConnectionBuilder()
                .WithUrl(this.EventStreamHubUrl)
                .WithAutomaticReconnect()
                .AddNewtonsoftJsonProtocol()
                .Build();

                await _hubConnection.StartAsync(cancellationToken);

                _logger?.LogInformation($"Finished starting event stream hub connection. Hub url: {this.EventStreamHubUrl}.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error starting event stream hub connection. Hub url: {this.EventStreamHubUrl}.");
            }
        }        

        public async ValueTask DisposeAsync()
        {
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
        }
    }
}
