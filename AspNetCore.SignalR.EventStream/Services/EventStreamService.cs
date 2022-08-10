using AspNetCore.SignalR.EventStream.DomainEntities;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Repositories;

namespace AspNetCore.SignalR.EventStream.Services
{
    public class EventStreamService : IEventStreamService
    {
        private readonly IRepository _repository;

        public EventStreamService(IRepository repository)
        {
            _repository = repository;
        }

        public async Task DeleteEventStreamAsync(long id)
        {
            await _repository.DeleteEventStreamAsync(id);
        }

        public async IAsyncEnumerable<EventStreamModel> SearchStreamsAsync(SearchStreamsModel model)
        {
            var eventStreams = await _repository.SearchEventStreams(new SearchEventStreamsEntity
            {
                Name = model.Name,
                StreamId = model.StreamId,
                CreatedStart = model.CreatedStart,
                CreatedEnd = model.CreatedEnd
            });

            foreach(var eventStream in eventStreams)
            {
                var eventStreamModel = new EventStreamModel
                {
                    Id = eventStream.Id,
                    StreamId = eventStream.StreamId,
                    Name = eventStream.Name,
                    LastAssociatedAt = eventStream.LastAssociatedAt,
                    LastEventInsertedAt = eventStream.LastEventInsertedAt,
                    CreatedAt = eventStream.CreatedAt
                };

                yield return eventStreamModel;
            }
        }

        public async Task<Guid> AssociateStreamsAsync(AssociateStreamsModel mergeStreamModel)
        {
            bool streamExists = false;

            if (mergeStreamModel.Existing)
            {
                if (!string.IsNullOrEmpty(mergeStreamModel.Name))
                {
                    streamExists = await _repository.DoesStreamExistAsync(mergeStreamModel.Name);
                }
                else
                {
                    streamExists = await _repository.DoesStreamExistAsync(mergeStreamModel.StreamId);
                }

                if (!streamExists)
                    throw new ApplicationException($"The stream {mergeStreamModel.Name ?? mergeStreamModel.StreamId.ToString() } does not exist.");
            }            

            foreach (var associatedStream in mergeStreamModel.AssociatedStreams)
            {
                if (!string.IsNullOrEmpty(associatedStream.Name))
                {
                    streamExists = await _repository.DoesStreamExistAsync(associatedStream.Name);
                }
                else
                {
                    streamExists = await _repository.DoesStreamExistAsync(associatedStream.StreamId);
                }

                if (!streamExists)
                    throw new ApplicationException($"The associated stream {associatedStream.Name ?? associatedStream.StreamId.ToString() } does not exist.");                                
            }

            Entities.EventStream stream;
            long streamId = 0;

            if (mergeStreamModel.Existing)
            {
                if (!string.IsNullOrEmpty(mergeStreamModel.Name))
                {
                    stream = await _repository.GetStreamAsync(mergeStreamModel.Name);                    
                }
                else
                {
                    stream = await _repository.GetStreamAsync(mergeStreamModel.StreamId);
                }
            }
            else
            {
                var sId = Guid.NewGuid();

                await _repository.AddAsync(new Entities.EventStream
                {
                    Name = mergeStreamModel.Name,
                    StreamId = sId                    
                });

                stream = await _repository.GetStreamAsync(sId);
            }

            streamId = stream.Id;
            var streamIdGuid = stream.StreamId;

            foreach (var associatedStream in mergeStreamModel.AssociatedStreams)
            {
                long associatedStreamId;

                if (!string.IsNullOrEmpty(associatedStream.Name))
                {
                    stream = await _repository.GetStreamAsync(associatedStream.Name);                    
                }
                else
                {
                    stream = await _repository.GetStreamAsync(associatedStream.StreamId);
                }

                associatedStreamId = stream.Id;

                var doesAssociationExist = await _repository.DoesEventStreamAssociationExistAsync(streamId, stream.Id);

                if (doesAssociationExist)
                    continue;

                await _repository.AddAsync(new Entities.EventStreamAssociation
                {
                    StreamId = streamId,
                    AssociatedStreamId = associatedStreamId
                });
            }

            return streamIdGuid;
        }
    }
}
