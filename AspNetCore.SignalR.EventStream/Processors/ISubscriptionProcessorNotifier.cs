using AspNetCore.SignalR.EventStream.Entities;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public interface ISubscriptionProcessorNotifier
    {
        event Func<IEnumerable<Event>, Task> OnEventsAdded;
        Task OnEventsAddedAsync(IEnumerable<Event> events);
    }
}
