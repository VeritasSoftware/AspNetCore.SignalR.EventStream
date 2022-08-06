# AspNetCore.SignalR.EventStream

## Event Streaming using SignalR web sockets

The framework allows you to build your own Event Stream Server.

And, from you Client application

you can

* Publish Events to a stream
* Subscribe to a stream
* Unsubscribe from a stream

using [SignalR client libraries](https://docs.microsoft.com/en-us/aspnet/core/signalr/client-features?view=aspnetcore-6.0) for .Net, Java, Javascript etc.

A normal flow would look like the below sequence diagram UML. 

![Event Stream Sequence Diagram UML](/Docs/NormalFlow-SequenceDiagramUML.jpg)

### Setting up your Server

You can create your own Event Stream Server.

To hook up Event Stream, do the following in the Startup.cs of your Server application.

```c#
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IEventStreamAuthorization, AuthorizationService>();

        services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
        {
            builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowAnyOrigin();
        }));

        //Hook up EventStreamHub using SignalR
        services.AddSignalR().AddNewtonsoftJsonProtocol(o =>
        {
            o.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        });

        //Add EventStream
        services.AddEventStream(options => 
        {
            options.UseSqlServer = true;
            options.SqlServerConnectionString = Configuration.GetConnectionString("EventStreamDatabase");
            options.EventStreamHubUrl = "https://localhost:5001/eventstreamhub";
        });

        services.AddControllers();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My Event Stream Server", Version = "v1" });
        });
    }

    public void Configure(IApplicationBuilder app)
    {
        //Use Event Stream
        app.UseEventStream();

        app.UseCors("CorsPolicy");

        app.UseSwagger();

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My Event Stream Server");
        });

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            //EventStreamHub endpoint
            endpoints.MapHub<EventStreamHub>("/eventstreamhub");
            endpoints.MapControllers();
        });
    }
```

#### Administration of Server

The Server hosts endpoints to perform various administrative tasks.

You can implement your own security for these endpoints. Read [**Authorization**](Docs/README_Authorization.md).

Out of the box, the Server can use **MS Sqlite** database.

You can also hook it up to use **MS Sql Server** database.

You can perform Admin tasks, while the Server is running,

Eg. You may want to associate multiple streams into a stream.

You can do so by calling the **associate** endpoint.

![Event Stream Server Admin Endpoints](/Docs/ServerAdminEndpoints.jpg)

#### Sample Server

There is a sample Server app included in the solution.

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
conn.On(receiveMethod, new Type[] { typeof(string), typeof(object) }, (arg1, arg2) =>
{
    var array = JsonConvert.DeserializeObject<object[]>(arg1[0].ToString());

    foreach(var arg in array)
    {
        dynamic parsedJson = JsonConvert.DeserializeObject(arg.ToString());
        var evt = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        Console.WriteLine($"Received Event from Stream {parsedJson.streamName}:");
        Console.WriteLine(evt);
    }    
    return Task.CompletedTask;
}, new object());
```

Then, call the **Subscribe** method on the Hub.

```c#
await conn.InvokeAsync("Subscribe", streamName, eventType, receiveMethod, subscriberId, subscriberKey, null);
```
The **SubscriberKey** is a Guid.

This key has to be provided when Unsubscribing.

#### Publish

To publish an Event, call the **Publish** method on the Hub.

```c#
await conn.InvokeAsync("Publish", streamName, new[]
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

![Event Stream Client](/Docs/Client.jpg)

### Event JSON

The Received Event JSON is as shown in example below:

```javascript
{
  "id": 5,
  "eventId": "24b15082-42ed-48e9-8465-a9ff678092ff",
  "originalEventId": null,
  "type": "MyEvent",
  "data": "eyJhIjoiMSJ9",
  "jsonData": null,
  "metaData": "e30=",
  "isJson": false,
  "streamId": "a2b7fc17-151a-436d-97a5-8b8febfd2776",
  "streamName": "MyStream"
}
```

## This project is a work in progress!
