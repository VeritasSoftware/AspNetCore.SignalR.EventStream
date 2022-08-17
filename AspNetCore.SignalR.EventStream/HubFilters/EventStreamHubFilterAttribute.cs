using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace AspNetCore.SignalR.EventStream.HubFilters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    internal class EventStreamHubFilterAttribute : Attribute, IHubFilter
    {
        public EventStreamHubFilterAttribute()
        {
        }

        public async ValueTask<object> InvokeMethodAsync(
                    HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object>> next)
        {
            var logger = invocationContext.ServiceProvider.GetServiceOrNull<ILogger<EventStreamHubFilterAttribute>>();

            logger?.LogInformation($"Calling hub method '{invocationContext.HubMethodName}'");

            var gatewayHubFilter = invocationContext.ServiceProvider.GetServiceOrNull<IEventStreamHubFilter>();

            if(gatewayHubFilter != null)
                await gatewayHubFilter.InvokeMethodAsync(invocationContext);

            try
            {
                return await next(invocationContext);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, $"Exception calling '{invocationContext.HubMethodName}'");
                throw;
            }
        }
        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
        {
            var logger = context.ServiceProvider.GetServiceOrNull<ILogger<EventStreamHubFilterAttribute>>();

            logger?.LogInformation($"Connected connection id: '{context.Context.ConnectionId}'");

            var gatewayHubFilter = context.ServiceProvider.GetServiceOrNull<IEventStreamHubFilter>();

            if (gatewayHubFilter != null)
                await gatewayHubFilter.OnConnectedAsync(context);

            await next(context);
        }

        public async Task OnDisconnectedAsync(
            HubLifetimeContext context, Exception exception, Func<HubLifetimeContext, Exception, Task> next)
        {
            var logger = context.ServiceProvider.GetServiceOrNull<ILogger<EventStreamHubFilterAttribute>>();

            logger?.LogInformation($"Disconnected connection id: '{context.Context.ConnectionId}'");

            //If user has not Unsubscribed, delete subscription on disconnect.
            var repository = context.ServiceProvider.GetServiceOrNull<IRepository>();
            await repository.DeleteSubscriptionAsync(context.Context.ConnectionId);

            var gatewayHubFilter = context.ServiceProvider.GetServiceOrNull<IEventStreamHubFilter>();

            if (gatewayHubFilter != null)
                await gatewayHubFilter.OnDisconnectedAsync(context, exception);

            await next(context, exception);
        }
    }
}
