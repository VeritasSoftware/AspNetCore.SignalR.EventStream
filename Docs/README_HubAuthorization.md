### Event Stream SignalR Hub Authorization

You can **authorize** the calls to the Event Stream SignalR Hub.

By implementing **policies** as shown below.

In your Server project, in Startup.cs,

In the **ConfigureServices** method:

```C#
    services.AddAuthentication();

    services.AddAuthorization(options =>
    {
        options.AddPolicy("EventStreamHub", policy =>   
        {
            //Set up your policy requirements here, for the Event Stream SignalR Hub
            //If you want anonymous access, use below requirement
            policy.AddRequirements(new AllowAnonymousAuthorizationRequirement());
        });
        options.AddPolicy("EventStreamHubPublish", policy =>
        {
            //Set up your policy requirements here, for the Event Stream SignalR Hub's Publish method
            //If you want anonymous access, use below requirement
            policy.AddRequirements(new AllowAnonymousAuthorizationRequirement());
        });
        options.AddPolicy("EventStreamHubSubscribe", policy =>
        {
            //Set up your policy requirements here, for the Event Stream SignalR Hub's Subscribe method
            //If you want anonymous access, use below requirement
            policy.AddRequirements(new AllowAnonymousAuthorizationRequirement());
        });
        options.AddPolicy("EventStreamHubUnsubscribe", policy =>
        {
            //Set up your policy requirements here, for the Event Stream SignalR Hub's Unsubscribe method
            //If you want anonymous access, use below requirement
            policy.AddRequirements(new AllowAnonymousAuthorizationRequirement());
        });
    });
```

And in the **Configure** method:

```C#
    app.UseAuthentication();
    app.UseAuthorization();
```