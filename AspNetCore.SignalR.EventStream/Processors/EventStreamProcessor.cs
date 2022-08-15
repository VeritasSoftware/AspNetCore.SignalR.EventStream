using AspNetCore.SignalR.EventStream.Repositories;

namespace AspNetCore.SignalR.EventStream.Processors
{
    public class EventStreamProcessor : IAsyncDisposable
    {
        private readonly IRepository _repository;
        private readonly ILogger<EventStreamLog>? _logger;

        private static Thread _processorThread = null;        

        public bool Start { get; set; } = false;

        public EventStreamProcessor(IRepository repository, ILogger<EventStreamLog>? logger = null)
        {
            _repository = repository;
            _logger = logger;
        }

        public void Process()
        {
            _processorThread = new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                /* run your code here */
                _logger?.LogInformation("Starting EventStreamProcessor process.");
                await this.ProcessAsync();
            });

            _logger?.LogInformation("Starting EventStreamProcessor thread.");
            _processorThread.Start();
        }

        private async Task ProcessAsync()
        {
            while (Start)
            {
                try
                {
                    var activeMergeStreams = await _repository.GetAssociatedStreams();

                    foreach (var activeMerge in activeMergeStreams)
                    {
                        try
                        {
                            var stream = await _repository.GetStreamAsync(activeMerge.StreamId);

                            if (stream != null)
                            {
                                //Get last merged datetime from stream
                                var lastAssociatedAt = stream.LastAssociatedEventId;

                                _logger?.LogInformation($"LastAssociatedEventId: {lastAssociatedAt}.");

                                if (activeMerge.AssociatedStreamIds != null && activeMerge.AssociatedStreamIds.Any())
                                {
                                    foreach (var associatedStreamId in activeMerge.AssociatedStreamIds)
                                    {
                                        //Fetch events from associated stream after last merged at
                                        var associatedStream = await _repository.GetStreamAsync(associatedStreamId, lastAssociatedAt);

                                        if (associatedStream != null && associatedStream.Events != null && associatedStream.Events.Any())
                                        {
                                            var events = new List<Entities.Event>();

                                            _logger?.LogInformation($"Adding new events ({associatedStream.Events.Count()}) from associated stream {associatedStream.Name} to stream {stream.Name}.");

                                            foreach (var @event in associatedStream.Events)
                                            {
                                                var @newEvent = new Entities.Event
                                                {                                                    
                                                    StreamId = stream.Id,
                                                    Data = @event.Data,
                                                    JsonData = @event.JsonData,
                                                    MetaData = @event.MetaData,
                                                    IsJson = @event.IsJson,
                                                    Type = @event.Type,
                                                    OriginalEventId = @event.EventId
                                                };

                                                 events.Add(@newEvent);                                                
                                            }
                                            
                                            //Create new Events
                                            await _repository.AddAsync(events.ToArray());

                                            _logger?.LogInformation($"Finished adding new events ({associatedStream.Events.Count()}) from associated stream {associatedStream.Name} to stream {stream.Name}.");                                            

                                            var lastEventId = events.Last().EventId;

                                            var lastEvent = await _repository.GetEventAsync(lastEventId);

                                            stream.LastAssociatedEventId = lastEvent?.Id;
                                            await _repository.UpdateAsync(stream);
                                        }                                        
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error in EventStreamProcessor thread.");
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in EventStreamProcessor thread.");
                    continue;
                }                
            }
        }

        public async ValueTask DisposeAsync()
        {
            Start = false;

            try
            {
                _logger?.LogInformation("Stopping thread.");
                _processorThread.Abort();
                _logger?.LogInformation("Finished stopping thread.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping thread.");
            }

            await Task.CompletedTask;
        }
    }
}
