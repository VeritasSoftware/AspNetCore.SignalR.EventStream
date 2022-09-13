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

## Documentation

https://github.com/VeritasSoftware/AspNetCore.SignalR.EventStream