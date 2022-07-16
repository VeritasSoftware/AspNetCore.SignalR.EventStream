# AspNetCore.SignalR.EventStream

## Event Streaming using SignalR web sockets

The framework allows you to build your own Event Stream Server.

And, from you Client application

you can

* Publish Events to a stream
* Subscribe to a stream
* Unsubscribe from a stream

using [SignalR client libraries](https://docs.microsoft.com/en-us/aspnet/core/signalr/client-features?view=aspnetcore-6.0) for .Net, Java, Javascript etc.

### Setting up your Server

You can create your own Event Stream Server.

To hook up Event Stream, do the following in the Startup.cs of your Server application.

```c#
        public void ConfigureServices(IServiceCollection services)
        {
            //Hook up EventStreamHub using SignalR
            services.AddSignalR().AddNewtonsoftJsonProtocol(o =>
            {
                o.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            //Add Event Stream
            services.AddEventStream();
        }

        public void Configure(IApplicationBuilder app)
        {
            //Use Event Stream
            app.UseEventStream(options => options.EventStreamHubUrl = "https://localhost:5001/eventstreamhub");

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                //Event Stream Hub endpoint
                endpoints.MapHub<EventStreamHub>("/eventstreamhub");
            });
        }
```

Run the sample Server app included in the solution.

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

```c#
//Variables
var subscriberId = Guid.NewGuid();
var subscriberKey = Guid.NewGuid();
var streamName = "MyStream";
var eventType = "MyEvent";

var receiveMethod = "ReceiveMyStreamEvent";
```

#### Subscribe

To subscribe to a stream, set up the **ReceiveMethod** event.

```c#
conn.On(receiveMethod, new Type[] { typeof(object), typeof(object) }, (arg1, arg2) =>
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
await conn.InvokeAsync("Subscribe", streamName, eventType, receiveMethod, subscriberId, subscriberKey, null);
```
The **SubscriberKey** is a Guid.

This key has to be provided when Publishing & Unsubscribing.

#### Publish

To publish an Event, call the **Publish** method on the Hub.

```c#
await conn.InvokeAsync("Publish", streamName, subscriberId, subscriberKey, new[]
{
    new {
        Type = eventType,
        Data = Encoding.UTF8.GetBytes("{\"a\":\"1\"}"),
        MetaData = Encoding.UTF8.GetBytes("{}"),
        IsJson = false
    }
});
```

#### Unsubscribe

To unsubscribe from a stream, call the **Unsubscribe** method on the Hub.

```c#
await conn.InvokeAsync("Unsubscribe", streamName, subscriberId, subscriberKey);
```

The Received Event JSON is as shown in example below:

```javascript
{
  "id": 5,
  "eventId": "24b15082-42ed-48e9-8465-a9ff678092ff",
  "type": "MyEvent",
  "data": "eyJhIjoiMSJ9",
  "jsonData": null,
  "metaData": "e30=",
  "isJson": false,
  "streamId": "a2b7fc17-151a-436d-97a5-8b8febfd2776",
  "streamName": "MyStream"
}
```

![Event Stream Client](/Docs/Client.jpg)

## This project is a work in progress!
