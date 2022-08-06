### Event Stream SignalR Hub Filter

You can filter the calls to the Event Stream SignalR Hub.

By implementing **IEventStreamHubFilter**.

In your Server project,

*	Create a service like below

```C#
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
```

*	Wire it up for dependency injection in Startup.cs

```C#
services.AddScoped<IEventStreamHubFilter, HubFilterService>();
```