# AspNetCore.SignalR.EventStream

## Event Streaming using SignalR web sockets

The framework allows you to build your own Event Stream Server.

You can run the Server in the solution too.

And, use a SignalR Client library,

to

* Publish Events to a stream
* Subscribe to a stream
* Unsubscribe from a stream

### Server

Run the Server app included in the solution.

![Event Stream Server](/Docs/Server.jpg)

### Setting up your Client in C#

Add a Nuget package (**Microsoft.AspNetCore.SignalR.Client**) to your project.

Then, build the connection to the **Event Stream Hub**.

```c#
var conn = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/eventstreamhub")
                .AddNewtonsoftJsonProtocol()
                .Build();

await conn.StartAsync();
```

#### Publish

To publish an Event, call the **Publish** method on the Hub.

```c#
await conn.InvokeAsync("Publish", streamName, new[]
{
    new {
        Type = "MyEvent",
        Data = Encoding.UTF8.GetBytes("{\"a\":\"1\"}"),
        MetaData = Encoding.UTF8.GetBytes("{}"),
        IsJson = false
    }
});
```

#### Subscribe

To subscribe to a stream, set up the Receive event.

```c#
conn.On("ReceiveMyStreamEvent", new Type[] { typeof(object), typeof(object) }, (arg1, arg2) =>
{
    dynamic parsedJson = JsonConvert.DeserializeObject(arg1[0].ToString());
    var evt = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
    Console.WriteLine($"Received Event from Stream {parsedJson.streamName}:");
    Console.WriteLine(evt);
    return Task.CompletedTask;
}, new object());

```

Then, call the **Subscribe** method on the Hub.

```c#
var subscriberId = Guid.NewGuid();
var subscriberKey = Guid.NewGuid();
var streamName = "MyStream";

Console.WriteLine($"Subscribing to Stream {streamName}.");
await conn.InvokeAsync("Subscribe", streamName, "MyEvent", "ReceiveMyStreamEvent", subscriberId, subscriberKey, null);

```
The **SubscriberKey** is a Guid.

This key has to be provided when Publishing & Unsubscribing.

#### Unsubscribe

To unsubscribe from a stream, call the **Unsubscribe** method on the Hub.

```c#
await conn.InvokeAsync("Unsubscribe", streamName, subscriberId, subscriberKey);
```

![Event Stream Client](/Docs/Client.jpg)

## This project is a work in progress!
