### Event Stream - Use your own Repository

You can use your own repository which talks to your database.

By implementing **IRepository** interface in your Server project.

![IRepository interface](/Docs/IRepository.jpg)

The, in the wire it up as below:

```C#
    //Add dependencies for MyRepository
    .
    .
    //Add Event Stream
    services.AddEventStream(options => 
    {        
        options.UseMyRepository = true;        
        options.Repository = typeof(MyRepository);
        options.EventStreamHubUrl = "https://localhost:5001/eventstreamhub";
    });
```

Add dependencies, your Repository needs, to DI.

Your repository is added as a Scoped service to DI by default.

But, if you set **RegisterMyRepository** to false, you have to add your Repository to DI too.

```C#
    options.RegisterMyRepository = false;
```