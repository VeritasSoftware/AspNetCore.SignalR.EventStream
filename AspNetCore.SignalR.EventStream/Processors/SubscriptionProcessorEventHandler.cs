using AspNetCore.SignalR.EventStream.Entities;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public class SubscriptionProcessorEventHandler : ISubscriptionProcessorEventHandler
    {
        public event Func<IEnumerable<Event>, Task> OnEventsAdded;

        public async Task OnEventsAddedAsync(IEnumerable<Event> events)
        {
            if (OnEventsAdded != null)
                await OnEventsAdded.Invoke(events);
        }
    }
}
