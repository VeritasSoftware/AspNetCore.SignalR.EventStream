using Microsoft.AspNetCore.Mvc.Filters;

namespace AspNetCore.SignalR.EventStream.Authorization
{
    public interface IEventStreamAuthorization
    {
        Task AuthorizeAsync(AuthorizationFilterContext context);
    }
}
