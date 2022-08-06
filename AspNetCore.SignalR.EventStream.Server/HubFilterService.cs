using AspNetCore.SignalR.EventStream.HubFilters;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.SignalR.EventStream.Server
{
    public class HubFilterService : IEventStreamHubFilter
    {
        public async ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext)
        {
            //Do your work here eg. to deny access
            //invocationContext.Context.Abort();

            return await Task.FromResult(0);
        }

        public async Task OnConnectedAsync(HubLifetimeContext context)
        {
            //Do your work here eg. to deny access
            //context.Context.Abort();

            await Task.CompletedTask;
        }

        public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception)
        {
            await Task.CompletedTask;
        }
    }
}
