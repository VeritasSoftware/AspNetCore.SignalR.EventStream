using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.SignalR.EventStream.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    internal class EventStreamAuthorizeAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        readonly IEventStreamAuthorization? _authorization = null;

        public EventStreamAuthorizeAttribute(IEventStreamAuthorization? authorization = null)
        {
            _authorization = authorization;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (this._authorization != null)
            {
                var logger = context.HttpContext.RequestServices.GetServiceOrNull<ILogger<EventStreamAuthorizeAttribute>>();

                logger?.LogInformation($"Calling Event Stream Authorization.");

                await this._authorization.AuthorizeAsync(context);

                logger?.LogInformation($"Finished calling Event Stream Authorization.");
            }
        }
    }
}
