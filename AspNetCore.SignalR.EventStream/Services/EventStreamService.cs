using AspNetCore.SignalR.EventStream.DomainEntities;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Processors;
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

        public async Task SetSubscriptionProcessorAsync(SetSubscriptionProcessorModel model)
        {
            if (EventStreamExtensions._subscriptionProcessor == null)
            {
                throw new InvalidOperationException($"{nameof(SubscriptionProcessor)} is null.");
            }

            if (model.Stop.HasValue && model.Stop.Value)
            {
                EventStreamExtensions._subscriptionProcessor.Start = false;
            }
            if (model.Start.HasValue && model.Start.Value)
            {
                if (!EventStreamExtensions._subscriptionProcessor.Start)
                {
                    EventStreamExtensions._subscriptionProcessor.Start = true;
                }                
            }

            await Task.CompletedTask;
        }

        public async Task<EventStreamModel> GetStreamAsync(Guid id)
        {
            var stream = await _repository.GetStreamAsync(id);

            if (stream == null)
                throw new InvalidOperationException($"Stream: {id} not found.");

            var model = new EventStreamModel
            {
                CreatedAt = stream.CreatedAt,
                Id = stream.Id,
                LastAssociatedEventId = stream.LastAssociatedEventId,
                Name = stream.Name,
                StreamId = stream.StreamId
            };

            return model;
        }

        public async Task<IEnumerable<EventStreamModel>> GetStreamsAsync(string name)
        {
            var streams = await _repository.GetStreamsAsync(name);

            if (streams == null || !streams.Any())
                throw new InvalidOperationException($"Stream with Name containing {name} not found.");

            var models = streams.Select(x => new EventStreamModel
            {
                CreatedAt = x.CreatedAt,
                Id = x.Id,
                LastAssociatedEventId = x.LastAssociatedEventId,
                Name = x.Name,
                StreamId = x.StreamId
            });

            return models;
        }

        public async Task<EventModel> GetEventAsync(long streamId, Guid id)
        {
            var @event = await _repository.GetEventAsync(streamId, id);

            if (@event == null)
                throw new InvalidOperationException($"Event: {id} not found.");

            return new EventModel
            {
                Data = @event.Data,
                Base64StringData = @event.Base64StringData,
                IsJson = @event.IsJson,
                IsBase64String = @event.IsBase64String,
                JsonData = @event.JsonData,
                MetaData = @event.MetaData,
                Base64StringMetaData = @event.Base64StringMetaData,
                StreamId = @event.Stream.StreamId,
                StreamName = @event.Stream.Name,
                Type = @event.Type,
                Description = @event.Description                
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
                LastAccessedCurrentEventId = subscriber.LastAccessedCurrentEventId,
                LastAccessedFromEventId = subscriber.LastAccessedFromEventId,
                LastAccessedToEventId = subscriber.LastAccessedToEventId,
                StreamId = subscriber.StreamId,
                Stream = new EventStreamModel
                {
                    Id = subscriber.Stream.Id,
                    StreamId = subscriber.Stream.StreamId,
                    CreatedAt = subscriber.Stream.CreatedAt,
                    Name = subscriber.Stream.Name,
                    LastAssociatedEventId = subscriber.Stream.LastAssociatedEventId
                }
            };

            return model;
        }

        public async Task UpdateSubscriberAsync(Guid subscriberId, UpdateSubscriberModel model)
        {
            var subscriber = await _repository.GetSubscriberAsync(subscriberId);

            if (subscriber == null)
                throw new InvalidOperationException($"Subscriber {subscriberId} not found.");

            subscriber.LastAccessedFromEventId = model.LastAccessedFromEventId > 0 ? model.LastAccessedFromEventId - 1 : model.LastAccessedFromEventId;
            subscriber.LastAccessedCurrentEventId = model.LastAccessedFromEventId > 0 ? model.LastAccessedFromEventId - 1 : model.LastAccessedFromEventId;
            subscriber.LastAccessedToEventId = model.LastAccessedToEventId;

            await _repository.UpdateAsync(subscriber);

            if (EventStreamExtensions._subscriptionProcessor != null)
                await EventStreamExtensions._subscriptionProcessor.ProcessSubscriber(subscriberId);
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

        public async IAsyncEnumerable<EventModel> SearchEventsAsync(SearchEventsModel searchEventsModel)
        {
            var events = await _repository.SearchEventsAsync(new SearchEventsEntity
            {
                CreatedEnd = searchEventsModel.CreatedEnd,
                CreatedStart = searchEventsModel.CreatedStart,
                MaxReturnRecords = searchEventsModel.MaxReturnRecords,
                StreamId = searchEventsModel.StreamId,
                Type = searchEventsModel.Type,
            });

            foreach (var e in events)
            {
                var eventModel = new EventModel
                {
                    Data = e.Data,
                    Base64StringMetaData = e.Base64StringMetaData,
                    IsJson = e.IsJson,
                    IsBase64String = e.IsBase64String,
                    JsonData = e.JsonData,
                    MetaData = e.MetaData,
                    Base64StringData = e.Base64StringData,
                    StreamId = e.Stream.StreamId,
                    StreamName = e.Stream.Name,
                    Type = e.Type,
                    Description = e.Description
                };

                yield return eventModel;
            }
        }
    }
}
