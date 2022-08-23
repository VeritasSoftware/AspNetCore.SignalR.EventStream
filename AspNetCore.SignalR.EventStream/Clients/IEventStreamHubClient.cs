using AspNetCore.SignalR.EventStream.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace AspNetCore.SignalR.EventStream.Clients
{
    public interface IEventStreamHubClient
    {
        HubConnection? HubConnection { get; }
        Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));
        Task SendAsync(EventStreamSubscriberModelResult modelResult);
    }
}
