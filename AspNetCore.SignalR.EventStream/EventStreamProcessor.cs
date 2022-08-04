using AspNetCore.SignalR.EventStream.Repositories;

namespace AspNetCore.SignalR.EventStream
{
    public class EventStreamProcessor
    {
        private readonly IRepository _repository;
        private static Thread _processorThread;

        public bool Start { get; set; } = false;

        public EventStreamProcessor(IRepository repository)
        {
            _repository = repository;
        }

        public void Process()
        {
            _processorThread = new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                /* run your code here */
                await this.ProcessAsync();
            });

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
                                var lastMergedAt = stream.LastAssociatedAt;

                                if (activeMerge.AssociatedStreamIds != null && activeMerge.AssociatedStreamIds.Any())
                                {
                                    foreach (var associatedStreamId in activeMerge.AssociatedStreamIds)
                                    {
                                        //Fetch events from associated stream after last merged at
                                        var associatedStream = await _repository.GetStreamAsync(associatedStreamId, lastMergedAt);

                                        if (associatedStream != null && associatedStream.Events != null && associatedStream.Events.Any())
                                        {
                                            var events = new List<Entities.Event>();

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

                                            //Create new Event
                                            await _repository.AddAsync(events.ToArray());

                                            lastMergedAt = DateTime.UtcNow;

                                            stream.LastAssociatedAt = lastMergedAt;
                                            await _repository.UpdateAsync(stream);
                                        }                                        
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //TODO: Log exception
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {

                }                
            }
        }
    }
}
