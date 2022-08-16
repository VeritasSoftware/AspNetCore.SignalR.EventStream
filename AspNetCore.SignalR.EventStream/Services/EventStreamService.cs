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

        public async Task<EventModel> GetEventAsync(long streamId, Guid id)
        {
            var @event = await _repository.GetEventAsync(streamId, id);

            if (@event == null)
                throw new InvalidOperationException($"Event: {id} not found.");

            return new EventModel
            {
                Data = @event.Data,
                IsJson = @event.IsJson,
                JsonData = @event.JsonData,
                MetaData = @event.MetaData,
                StreamId = @event.Stream.StreamId,
                StreamName = @event.Stream.Name,
                Type = @event.Type
            };
        }

        public async Task<EventStreamSubscriberModel> GetSubscriberAsync(Guid subscriberId)
        {
            var subscriber = await _repository.GetSubscriberAsync(subscriberId);

            if (subscriber == null)
            {
                throw new InvalidOperationException($"Subscriber: {subscriberId} not found.");
            }

            var model = new EventStreamSubscriberModel
            {
                Id = subscriber.Id,
                SubscriberId = subscriber.SubscriberId,
                ConnectionId = subscriber.ConnectionId,
                CreatedAt = subscriber.CreatedAt,
                LastAccessedEventId = subscriber.LastAccessedEventId,
                ReceiveMethod = subscriber.ReceiveMethod,
                StreamId = subscriber.StreamId,
                Stream = new EventStreamModel
                {
                    Id = subscriber.Stream.Id,
                    StreamId = subscriber.Stream.StreamId,
                    CreatedAt = subscriber.Stream.CreatedAt,
                    Name = subscriber.Stream.Name
                }
            };

            return model;
        }

        public async Task UpdateSubscriberAsync(Guid subscriberId, UpdateSubscriberModel model)
        {
            if (model.LastAccessedEventId.HasValue)
            {
                var subscriber = await _repository.GetSubscriberAsync(subscriberId);

                var @event = await _repository.GetEventAsync(subscriber.StreamId, model.LastAccessedEventId.Value);

                await _repository.UpdateSubscriptionLastAccessedAsync(subscriberId, @event.Id);
            }
        }

        public async Task DeleteEventStreamAsync(long id)
        {
            await _repository.DeleteEventStreamAsync(id);
        }

        public async IAsyncEnumerable<EventStreamModel> SearchStreamsAsync(SearchStreamsModel model)
        {
            var eventStreams = await _repository.SearchEventStreamsAsync(new SearchEventStreamsEntity
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
                    LastAssociatedEventId = eventStream.LastAssociatedEventId,
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
