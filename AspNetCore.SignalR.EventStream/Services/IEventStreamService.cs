using AspNetCore.SignalR.EventStream.Models;

namespace AspNetCore.SignalR.EventStream.Services
{
    public interface IEventStreamService
    {
        Task<Guid> AssociateStreamsAsync(AssociateStreamsModel mergeStreamModel);
        IAsyncEnumerable<EventStreamModel> SearchStreamsAsync(SearchStreamsModel model);
        Task DeleteEventStreamAsync(long id);
        Task UpdateSubscriberAsync(Guid subscriberId, UpdateSubscriberModel subscriberLastAccessedAtModel);
        Task<EventStreamSubscriberModel> GetSubscriberAsync(Guid subscriberId);
        Task<EventModel> GetEventAsync(long streamId, Guid id);
        Task<EventStreamModel> GetStreamAsync(Guid id);
        Task<IEnumerable<EventStreamModel>> GetStreamsAsync(string name);
    }
}