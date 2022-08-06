using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.SignalR.EventStream.HubFilters
{
    public interface IEventStreamHubFilter
    {
        ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext);
        Task OnConnectedAsync(HubLifetimeContext context);
        Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception);
    }
}
