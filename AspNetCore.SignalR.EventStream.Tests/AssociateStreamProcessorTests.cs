using AspNetCore.SignalR.EventStream.Entities;
using AspNetCore.SignalR.EventStream.Processors;
using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.SignalR.EventStream.Tests
{
    [Collection("EventStreamUnitTests")]
    public class AssociateStreamProcessorTests
    {
        IServiceProvider ServiceProvider { get; }

        public AssociateStreamProcessorTests()
        {
            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build();

            services.AddEventStream(o =>
            {
                o.DatabaseType = DatabaseTypeOptions.Sqlite;
                o.ConnectionString = config.GetConnectionString("EventStreamDatabase");
            });

            ServiceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Process_Success()
        {
            //Arrange
            var context = ServiceProvider.GetRequiredService<SqliteDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            //Start processor
            var processor = ServiceProvider.GetRequiredService<IAssociateStreamProcessor>();
            processor.Start = true;

            var repository = ServiceProvider.GetRequiredService<IRepository>();

            //Stream info
            var streamId = Guid.NewGuid();
            var streamName = "MyStream";
            var streamType = "MyEvent";
            var eventId = Guid.NewGuid();

            //Associated stream info
            var streamId1 = Guid.NewGuid();
            var streamName1 = "MyAssociatedStream";
            var streamType1 = "MyAssociatedEvent";
            var eventId1 = Guid.NewGuid();

            //Create stream
            await repository.AddAsync(new Entities.EventStream
            {
                Name = streamName,
                StreamId = streamId
            });

            //Create associated stream
            await repository.AddAsync(new Entities.EventStream
            {
                Name = streamName1,
                StreamId = streamId1
            });

            var stream = await repository.GetStreamAsync(streamName);
            var stream1 = await repository.GetStreamAsync(streamName1);

            //Create association
            await repository.AddAsync(new EventStreamAssociation
            {
                StreamId = stream.Id,
                AssociatedStreamId = stream1.Id
            });

            //Add Events to Associated stream
            await repository.AddAsync(new Event
            {
                StreamId = stream1.Id,
                EventId = eventId1,
                Type = streamType1,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"1\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            },
            new Event
            {
                StreamId = stream1.Id,
                EventId = eventId1,
                Type = streamType1,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"2\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            });

            //Get stream
            stream = await repository.GetStreamAsync(stream.Id, 0);

            //Asserts
            Assert.Equal(2, stream.Events.Count);
            Assert.True(stream.Events.All(e => e.OriginalEventId != null));
        }
    }
}
