### Event Stream SignalR Hub Authorization

You must **authorize** the calls to the Event Stream SignalR Hub.

By implementing **policies** as shown below.

In your Server project, in Startup.cs,

In the **ConfigureServices** method:

```C#
    services.AddAuthentication();

    //Set up your Authorization policy requirements here, for the Event Stream SignalR Hub
    //If you want anonymous access, use below AllowAnonymousAuthorizationRequirement
    services.AddAuthorization(builder => builder.AddHubAuthorizationPolicy(new AllowAnonymousAuthorizationRequirement())
                                                .AddHubPublishAuthorizationPolicy(new AllowAnonymousAuthorizationRequirement())
                                                .AddHubSubscribeAuthorizationPolicy(new AllowAnonymousAuthorizationRequirement())
                                                .AddHubUnsubscribeAuthorizationPolicy(new AllowAnonymousAuthorizationRequirement()));
```

**Note:-** You can use the Event Stream AddAuthorization extension or you can create your own.

If you want to create your own, you have to implement below policies:

* EventStreamHubPolicy
* EventStreamHubPublishPolicy
* EventStreamHubSubscribePolicy
* EventStreamHubUnsubscribePolicy

Eg.

```C#
    services.AddAuthorization(options => 
    {
        options.AddPolicy("EventStreamHubPolicy", policy =>
        {
            //Add your requirements etc here.
        });
    });
```

And in the **Configure** method:

```C#
    app.UseAuthentication();
    app.UseAuthorization();
```