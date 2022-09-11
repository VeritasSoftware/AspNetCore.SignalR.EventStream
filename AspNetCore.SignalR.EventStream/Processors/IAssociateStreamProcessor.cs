
namespace AspNetCore.SignalR.EventStream.Processors
{
    public interface IAssociateStreamProcessor
    {
        string Name { get; }
        bool Start { get; set; }
    }
}