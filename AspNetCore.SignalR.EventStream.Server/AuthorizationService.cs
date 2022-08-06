using AspNetCore.SignalR.EventStream.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.SignalR.EventStream.Server
{
    public class AuthorizationService : IEventStreamAuthorization
    {
        public Task AuthorizeAsync(AuthorizationFilterContext context)
        {
            //Put your authorization here

            return Task.CompletedTask;
        }
    }
}
