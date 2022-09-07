
namespace AspNetCore.SignalR.EventStream.Processors
{
    public interface IEventStreamProcessor
    {
        string Name { get; }
        bool Start { get; set; }

        ValueTask DisposeAsync();
    }
}