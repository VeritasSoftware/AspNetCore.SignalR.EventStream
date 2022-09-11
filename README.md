# AspNetCore.SignalR.EventStream

## .Net Event Sourcing framework using SignalR web sockets

**Event sourcing** stores the state of a database object as a sequence of events – essentially a new event for each time the object changed state, from the beginning of the object’s existence.

Read more on Event Sourcing [here](/Docs/README_EventSourcing.md).

**SignalR** uses web sockets for real-time, 2-way communication between Client & Server.

Read more on SignalR:

* Introduction to SignalR [here](https://docs.microsoft.com/en-us/aspnet/signalr/overview/getting-started/introduction-to-signalr)
* Overview of ASP.NET Core SignalR [here](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-6.0).

The framework implements Event Sourcing using SignalR web sockets as the communication protocol.

### Advantages of using SignalR web sockets:

* Browser based web app Clients can easily communicate with the Server. Besides, normal apps.
* Web sockets only need an internet connection for Client-Server communication. So, no special network infrastructure is needed.
* You can leverage the built in security.
* SignalR is an industry standard. With client libraries available for all platforms like Javascript, Java, Python, Mac OS, Windows etc.

## Framework

This framework allows you to build your own Event Stream Server.

### Features

* SignalR web sockets interface with client.
* Multiple databases supported.
    * MS Sqlite (out of the box)
    * MS Sql Server
    * Azure CosmosDb
* Http endpoints to administer the Server.
* Ability to replay events in a stream.
* Implement your own security (as you like) in the Authorization hooks.
* Implement your own logging (as you like).

And, from you Client application

you can

* Publish Events to a stream
* Subscribe to a stream
* Unsubscribe from a stream

using [SignalR client libraries](https://docs.microsoft.com/en-us/aspnet/core/signalr/client-features?view=aspnetcore-6.0) for .Net, Java, Javascript etc.

A normal flow would look like the below sequence diagram UML. 

![Event Stream Sequence Diagram UML](/Docs/NormalFlow-SequenceDiagramUML.jpg)

## Setting up your Server

You can create your own Event Stream Server.

Your Server can be any project with **Microsoft.NET.Sdk.Web** Sdk.

Even a Console app, with this Sdk, will do.

To hook up Event Stream, do the following in the Startup.cs of your Server application.

```c#
    public void ConfigureServices(IServiceCollection services)
    {
        //Event Stream SignalR Hub Filter
        services.AddScoped<IEventStreamHubFilter, HubFilterService>();

        //Event Stream Admin Http endpoints security
        services.AddScoped<IEventStreamAuthorization, AuthorizationService>();

        //Event Stream SignalR Hub security
        services.AddAuthentication();

        //Set up your Authorization policy requirements here, for the Event Stream SignalR Hub
        //If you want anonymous access, use below AllowAnonymousAuthorizationRequirement
        services.AddAuthorization(builder => builder.AddHubPolicyRequirements(new AllowAnonymousAuthorizationRequirement())
                                                    .AddHubPublishPolicyRequirements(new AllowAnonymousAuthorizationRequirement())
                                                    .AddHubSubscribePolicyRequirements(new AllowAnonymousAuthorizationRequirement())
                                                    .AddHubUnsubscribePolicyRequirements(new AllowAnonymousAuthorizationRequirement()));

        //Set up CORS as you want
        services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
        {
            builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowAnyOrigin();
        }));

        //Hook up Event Stream Hub using SignalR
        services.AddSignalR().AddNewtonsoftJsonProtocol(o =>
        {
            o.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        });

        //Add Event Stream
        services.AddEventStream(options => 
        {
            options.DatabaseType = DatabaseTypeOptions.SqlServer;
            options.ConnectionString = Configuration.GetConnectionString("EventStreamDatabase");
            options.DeleteDatabaseIfExists = false;
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

        //Use security
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            //Event Stream Hub endpoint
            endpoints.MapHub<EventStreamHub>("/eventstreamhub");
            endpoints.MapControllers();
        });
    }
```

In your Server project's **appsettings.json** add a **secret key**.

And, you must specify the database connection string.

Eg.

```C#
{
  "EventStreamSecretKey": "fce17eec-4913-48d6-b013-2583ab8583b3",
  "ConnectionStrings": {
    //Sqlite
    //"EventStreamDatabase": "Data Source=.\\eventstreamdb.db"
    //MS Sql Server
    "EventStreamDatabase": "Server=localhost;Database=EventStream;Trusted_Connection=True;"
    //Azure CosmosDb
    //"EventStreamDatabase": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
}
```

Out of the box, the Server can use **MS Sqlite** database.

You can also hook it up to use **MS Sql Server** and **Azure CosmosDb** databases.

But, you can implement **IRepository** interface to hook the Server up to your own database. 

Read [**Repositories**](Docs/README_Repositories.md).

You can filter requests to the Event Stream SignalR Hub. Read [**Hub Filter**](Docs/README_HubFilter.md).

You can also implement access to the Hub in this filter.

The SignalR Hub is secured by default. With **policies**.

In your Server, you can set up **Authorization** to the SignalR Hub. Read [**Hub Authorization**](Docs/README_HubAuthorization.md).

### Administration of Server

The Server hosts endpoints to perform various administrative tasks.

You can implement your own security for these endpoints. Read [**Authorization**](Docs/README_Authorization.md).

You can perform Admin tasks, while the Server is running,

Eg. You may want to get a stream by it's id.

You can do so by calling the **streams/{id}** endpoint.

![Event Stream Server Admin Endpoints](/Docs/ServerAdminEndpoints.jpg)

You can replay events in a stream. Read [**Replay events**](Docs/README_Rewind_FastForward.md).

### Sample Server

There is a sample Server app included in the solution.

![Event Stream Server](/Docs/Server.jpg)

## Setting up your Client in C#

Add a Nuget package (**Microsoft.AspNetCore.SignalR.Client**) to your project.

Then, build the connection to the **Event Stream Hub**.

```c#
var conn = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/eventstreamhub")
                .WithAutomaticReconnect()
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
```

### Subscribe

To subscribe to a stream, set up the **StreamName** as the event name.

The Server can stream multiple events (in an JSON string array).

You can access these events in the handler as shown below.

```c#
conn.On(streamName, new Type[] { typeof(string), typeof(object) }, (arg1, arg2) =>
{
    var events = JsonConvert.DeserializeObject<object[]>(arg1[0].ToString());

    foreach(var @event in events)
    {
        dynamic parsedJson = JsonConvert.DeserializeObject(@event.ToString());
        var evt = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        Console.WriteLine($"Received Event from Stream {parsedJson.StreamName}:");
        Console.WriteLine(evt);
    }    
    return Task.CompletedTask;
}, new object());
```

Then, call the **Subscribe** method on the Hub.

```c#
await conn.InvokeAsync("Subscribe", new
{
    StreamName = streamName,
    SubscriberId = subscriberId,
    SubscriberKey = subscriberKey
});
```
The **SubscriberId** & **SubscriberKey** are Guids.

This key has to be provided when Unsubscribing.

### Publish

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

### Unsubscribe

To unsubscribe from a stream, call the **Unsubscribe** method on the Hub.

```c#
await conn.InvokeAsync("Unsubscribe", streamName, subscriberId, subscriberKey);
```

### Sample Client applications:

The Client Console app and the Client Web app,

are both subscribing to the same stream.

The Console app, also, publishes Events to the stream.

Both the apps, receive the Events in real-time.

![Event Stream Clients](/Docs/Clients.jpg)

## Event JSON

The Received Event JSON is as shown in example below:

```javascript
{
  "Id": 1,
  "EventId": "9233420c-bd4c-40b4-8015-c822944b24b1",
  "OriginalEventId": null,
  "CreatedAt": "2022-09-04T16:08:33.134102+10:00",
  "Description": null,
  "Type": "MyEvent0",
  "Data": "eyJhIjoiMSJ9",
  "Base64StringData": null,
  "JsonData": null,
  "MetaData": "e30=",
  "Base64StringMetaData": null,
  "IsJson": false,
  "IsBase64String": false,
  "StreamId": "ef897228-9fda-41dd-bd4d-6659b70b3983",
  "StreamName": "MyStream"
}
```

## This project is a work in progress!
