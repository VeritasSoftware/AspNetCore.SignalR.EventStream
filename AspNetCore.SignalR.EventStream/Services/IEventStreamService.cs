using AspNetCore.SignalR.EventStream.Models;

namespace AspNetCore.SignalR.EventStream.Services
{
    public interface IEventStreamService
    {
        Task<Guid> AssociateStreamsAsync(AssociateStreamsModel mergeStreamModel);
        IAsyncEnumerable<EventStreamModel> SearchStreamsAsync(SearchStreamsModel model);
        Task DeleteEventStreamAsync(long id);
    }
}