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
                await this._authorization.AuthorizeAsync(context);
            }
        }
    }
}
