// See https://aka.ms/new-console-template for more information
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;

Console.WriteLine("Event Stream Client");
Console.WriteLine(Environment.NewLine);

var conn = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/eventstreamhub")
                .AddNewtonsoftJsonProtocol()
                .Build();

conn.On("ReceiveMyStreamEvent", new Type[] { typeof(object), typeof(object) }, (arg1, arg2) =>
{
    dynamic parsedJson = JsonConvert.DeserializeObject(arg1[0].ToString());
    var evt = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
    Console.WriteLine($"Received Event from Stream {parsedJson.streamName}:");
    Console.WriteLine(evt);
    return Task.CompletedTask;
}, new object());

await conn.StartAsync();

var subscriberId = Guid.NewGuid();
var subscriberKey = Guid.NewGuid();
var streamName = "MyStream";

Console.WriteLine($"Subscribing to Stream {streamName}.");
await conn.InvokeAsync("Subscribe", streamName, "MyEvent", "ReceiveMyStreamEvent", subscriberId, subscriberKey, null);

Console.WriteLine($"Publishing to Stream {streamName}.");
await conn.InvokeAsync("Publish", streamName, subscriberId, subscriberKey, new[]
{
    new {
        Type = "MyEvent",
        Data = Encoding.UTF8.GetBytes("{\"a\":\"1\"}"),
        MetaData = Encoding.UTF8.GetBytes("{}"),
        IsJson = false
    }
});

Thread.Sleep(500);

Console.WriteLine($"Unsubscribing from Stream {streamName}.");
await conn.InvokeAsync("Unsubscribe", streamName, subscriberId, subscriberKey);

Console.ReadLine();
