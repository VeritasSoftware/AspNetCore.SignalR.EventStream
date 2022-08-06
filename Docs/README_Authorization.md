### Event Stream Administration Endpoints Authorization

The library provides an interface, your Server can implement, to hook into the endpoint authorization.

In your Server project, hook up your Authentication and Authorization as you want.

You can now authorize using the **Event Stream Authorization Filter**.

### Example

In your Server project,

*	Create a service like below

```C#
    public class AuthorizationService : IEventStreamAuthorization
    {
        public Task AuthorizeAsync(AuthorizationFilterContext context)
        {
            //Put your authorization here

            //Eg. to prevent access
            //context.Result = new UnauthorizedResult();

            return Task.CompletedTask;
        }
    }
```

See **AuthorizationFilterContext** [here](https://docs.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.filters.authorizationfiltercontext?view=aspnetcore-6.0).


*	Wire it up for dependency injection in Startup.cs

```C#
services.AddScoped<IEventStreamAuthorization, AuthorizationService>();
```