using AspNetCore.SignalR.EventStream.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.SignalR.EventStream.Server
{
    public class AuthorizationService : IEventStreamAuthorization
    {
        public Task AuthorizeAsync(AuthorizationFilterContext context)
        {
            //Put your authorization here

            //Eg. to prevent access
            //context.Result = new UnauthorizedResult();

            return Task.CompletedTask;
        }
    }
}
