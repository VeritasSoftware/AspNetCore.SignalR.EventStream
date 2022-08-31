using AspNetCore.SignalR.EventStream.Entities;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public interface INotifier
    {
        event Func<IEnumerable<Event>, Task> OnEventsAdded;
        Task OnEventsAddedAsync(IEnumerable<Event> events);
    }

    public interface ISubscriptionProcessorNotifier : INotifier
    {

    }

    public interface IEventStreamProcessorNotifier : INotifier
    {

    }
}
