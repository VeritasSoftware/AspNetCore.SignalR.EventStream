using AspNetCore.SignalR.EventStream.Entities;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public interface ISubscriptionProcessorEventHandler
    {
        event Func<IEnumerable<Event>, Task> OnEventsAdded;
        Task OnEventsAddedAsync(IEnumerable<Event> events);
    }
}
