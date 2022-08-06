using AspNetCore.SignalR.EventStream.Models;

namespace AspNetCore.SignalR.EventStream.Services
{
    public interface IEventStreamService
    {
        Task<Guid> MergeStreams(AssociateStreamsModel mergeStreamModel);
        Task<IEnumerable<EventStreamModel>> SearchStreams(SearchStreamsModel model);
    }
}