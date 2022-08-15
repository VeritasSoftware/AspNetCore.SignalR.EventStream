using AspNetCore.SignalR.EventStream.Entities;
using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.SignalR.EventStream.Tests
{
    public class RepositoryTests
    {
        IServiceProvider ServiceProvider { get; }

        public RepositoryTests()
        {
            var services = new ServiceCollection();

            services.AddEventStream();

            ServiceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task GetStreamAsync_Success()
        {
            var context = ServiceProvider.GetRequiredService<SqliteDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var repository = ServiceProvider.GetRequiredService<IRepository>();

            var streamId = Guid.NewGuid();
            var streamName = "MyStream";
            var streamType = "MyEvent";
            var eventId = Guid.NewGuid();
            var eventId1 = Guid.NewGuid();

            await repository.AddAsync(new Entities.EventStream
            {
                Name = streamName,
                StreamId = streamId
            });

            var myStream = await repository.GetStreamAsync(streamId);

            await repository.AddAsync(new Event
            {
                StreamId = myStream.Id,
                EventId = eventId,
                Type = streamType,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"1\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            });

            Thread.Sleep(1000);

            await repository.AddAsync(new Entities.Event
            {
                StreamId = myStream.Id,
                EventId = eventId1,
                Type = streamType,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"2\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            });

            var myStream1 = await repository.GetStreamAsync(streamName);

            var stream = await repository.GetStreamAsync(myStream1.Id, 1);

            var events = stream.Events;

            Assert.Equal(streamId, stream.StreamId);
            Assert.Single(events);
            Assert.Equal(eventId1, events.First().EventId);
            Assert.Equal(streamId, events.First().Stream.StreamId);
            Assert.Equal(streamName, events.First().Stream.Name);
            Assert.Equal(streamType, events.First().Type);
        }

        [Fact]
        public async Task GetSubscriberAsync_Success()
        {
            var context = ServiceProvider.GetRequiredService<SqliteDbContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            var repository = ServiceProvider.GetRequiredService<IRepository>();

            var streamId = Guid.NewGuid();
            var streamName = "MyStream";
            var streamType = "MyEvent";
            var eventId = Guid.NewGuid();
            var eventId1 = Guid.NewGuid();
            var subsciberId = Guid.NewGuid();

            await repository.AddAsync(new Entities.EventStream
            {
                Name = streamName,
                StreamId = streamId
            });

            var myStream = await repository.GetStreamAsync(streamId);

            await repository.AddAsync(new Entities.Event
            {
                StreamId = myStream.Id,
                EventId = eventId,
                Type = streamType,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"1\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            });

            Thread.Sleep(1000);

            await repository.AddAsync(new Event
            {
                StreamId = myStream.Id,
                EventId = eventId1,
                Type = streamType,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"2\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            });

            await repository.AddAsync(new EventStreamSubscriber
            {
                StreamId = myStream.Id,
                SubscriberId = subsciberId,
                ConnectionId = "AjsjslwAsssaAAAsVVV==",
                LastAccessedEventId = 1
            });

            //Act
            var subscriber = await repository.GetSubscriberAsync(subsciberId, 1);

            var events = subscriber.Stream.Events;

            //Assert
            Assert.Equal(subsciberId, subscriber.SubscriberId);
            Assert.Equal(streamId, subscriber.Stream.StreamId);
            Assert.Single(events);
            Assert.Equal(eventId1, events.First().EventId);
            Assert.Equal(streamId, events.First().Stream.StreamId);
            Assert.Equal(streamName, events.First().Stream.Name);
            Assert.Equal(streamType, events.First().Type);
        }
    }
}