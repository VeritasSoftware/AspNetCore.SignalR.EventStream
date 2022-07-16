using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.EventStream
{
    public interface IEventStreamHubFilter
    {
        ValueTask<object> InvokeMethodAsync(HubInvocationContext invocationContext);
        Task OnConnectedAsync(HubLifetimeContext context);
        Task OnDisconnectedAsync(HubLifetimeContext context, Exception exception);
    }
}
