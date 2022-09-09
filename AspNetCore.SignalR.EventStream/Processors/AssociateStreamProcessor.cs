using AspNetCore.SignalR.EventStream.Repositories;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public class AssociateStreamProcessor : IAsyncDisposable, IAssociateStreamProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAssociateStreamProcessorNotifier _notifier;
        private readonly ILogger<AssociateStreamProcessor>? _logger;

        private bool _start = false;

        public bool Start
        {
            get
            {
                return _start;
            }
            set
            {
                if (!value)
                {
                    _logger?.LogInformation("Detaching On Events Added Notifier.");
                    _notifier.OnEventsAdded -= OnEventsAddedHandler;
                    _logger?.LogInformation("Finished detaching On Events Added Notifier.");
                    _logger?.LogInformation($"{Name} stopped.");
                }
                else
                {
                    _logger?.LogInformation("Attaching On Events Added Notifier.");
                    _notifier.OnEventsAdded += OnEventsAddedHandler;
                    _logger?.LogInformation("Finished attaching On Events Added Notifier.");
                    _logger?.LogInformation($"{Name} started.");
                }

                _start = value;
            }
        }
        public string Name => nameof(AssociateStreamProcessor);

        public AssociateStreamProcessor(IServiceProvider serviceProvider, IAssociateStreamProcessorNotifier notifier, ILogger<AssociateStreamProcessor>? logger = null)
        {
            _serviceProvider = serviceProvider;
            _notifier = notifier;
            _logger = logger;
        }

        private async Task OnEventsAddedHandler(IEnumerable<Entities.Event> events)
        {
            try
            {
                var repository = _serviceProvider.GetRequiredService<IRepository>();

                var streamId = events.First().StreamId;
                var mergeStream = await repository.GetStreamAsync(streamId);
                var streamName = mergeStream.Name;

                var activeMergeStreams = await repository.GetAssociatedStreamsAsync(streamId);

                foreach (var activeMerge in activeMergeStreams)
                {
                    try
                    {
                        if (activeMerge.AssociatedStreamIds != null && activeMerge.AssociatedStreamIds.Any())
                        {
                            var stream = await repository.GetStreamAsync(activeMerge.StreamId);

                            if (stream == null)
                            {
                                throw new InvalidOperationException($"Stream {activeMerge.StreamId} not found.");
                            }

                            foreach (var associatedStreamId in activeMerge.AssociatedStreamIds)
                            {
                                //Fetch events from associated stream after last merged at

                                if (events.Any())
                                {
                                    var eventEntities = new List<Entities.Event>();

                                    _logger?.LogInformation($"Adding new events ({events.Count()}) from associated stream {streamName} to stream {stream.Name}.");

                                    foreach (var @event in events)
                                    {
                                        var @newEvent = new Entities.Event
                                        {
                                            StreamId = activeMerge.StreamId,
                                            Data = @event.Data,
                                            JsonData = @event.JsonData,
                                            MetaData = @event.MetaData,
                                            IsJson = @event.IsJson,
                                            Type = @event.Type,
                                            OriginalEventId = @event.EventId
                                        };

                                        eventEntities.Add(@newEvent);
                                    }

                                    //Create new Events
                                    await repository.AddAsync(eventEntities.ToArray());

                                    _logger?.LogInformation($"Finished adding new events ({events.Count()}) from associated stream {streamName} to stream {stream.Name}.");

                                    var lastEventId = eventEntities.Last().EventId;

                                    var lastEvent = await repository.GetEventAsync(stream.Id, lastEventId);

                                    stream.LastAssociatedEventId = lastEvent?.Id;
                                    await repository.UpdateAsync(stream);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"Error in {Name} thread.");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Error in {Name} thread.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            Start = false;

            await Task.CompletedTask;
        }
    }
}
