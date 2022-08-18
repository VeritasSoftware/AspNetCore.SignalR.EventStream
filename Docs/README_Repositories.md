### Event Stream - Repositories

Below databases are supported:

* MS Sqlite (out of the box)
* MS Sql Server
* Azure CosmosDb

You can configure which database you want to use, like below:

```C#
    //Add Event Stream
    services.AddEventStream(options => 
    {
        options.DatabaseType = DatabaseTypeOptions.SqlServer;
        options.ConnectionString = Configuration.GetConnectionString("EventStreamDatabase");
        options.DeleteDatabaseIfExists = false;
        options.EventStreamHubUrl = "https://localhost:5001/eventstreamhub";
    });
```

Specify the **DatabaseTypeOptions**.

And, provide **ConnectionString**.

Setting **DeleteDatabaseIfExists** to **true**, deletes the database, on Server start up. Default is **false**.

The **appsettings.json** of the Server, has to have a section like below:

```javascript
  "ConnectionStrings": {
    //Sqlite
    //"EventStreamDatabase": "Data Source=.\\eventstreamdb.db"
    //MS Sql Server
    "EventStreamDatabase": "Server=localhost;Database=EventStream;Trusted_Connection=True;"
    //Azure CosmosDb
    //"EventStreamDatabase": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
  },
```

## Create your own Repository

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