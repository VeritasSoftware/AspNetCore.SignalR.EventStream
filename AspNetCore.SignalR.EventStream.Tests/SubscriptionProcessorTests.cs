using AspNetCore.SignalR.EventStream.Clients;
using AspNetCore.SignalR.EventStream.Entities;
using AspNetCore.SignalR.EventStream.Models;
using AspNetCore.SignalR.EventStream.Processors;
using AspNetCore.SignalR.EventStream.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.SignalR.EventStream.Tests
{
    [Collection("EventStreamUnitTests")]
    public class SubscriptionProcessorTests
    {
        IServiceProvider ServiceProvider { get; }

        public SubscriptionProcessorTests()
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

            var streamId = Guid.NewGuid();
            var streamName = "MyStream";
            var streamType = "MyEvent";
            var eventId = Guid.NewGuid();
            var eventId1 = Guid.NewGuid();
            var subscriberId = Guid.NewGuid();
            var subscriberKey = Guid.NewGuid();
            var subscriberId1 = Guid.NewGuid();
            var subscriberKey1 = Guid.NewGuid();

            var mockLogger = new Mock<ILogger<SubscriptionProcessor>>();

            mockLogger.Setup(m => m.Log(LogLevel.Information, 1000, $"Finished streaming events (2) to subscribers (2) of stream {streamName}.", null, null)).Verifiable();
            mockLogger.Setup(m => m.Log(LogLevel.Information, 1000, $"Finished streaming events (1) to subscribers (2) of stream {streamName}.", null, null)).Verifiable();

            var mockHubClient = new Mock<IEventStreamHubClient>();
            mockHubClient.SetupGet(x => x.IsConnected).Returns(true);
            mockHubClient.Setup(x => x.SendAsync(It.IsAny<EventStreamSubscriberModelResult>())).Returns(Task.CompletedTask);

            var repository = ServiceProvider.GetRequiredService<IRepository>();
            var repository1 = ServiceProvider.GetRequiredService<IRepository>();

            var subscriptionProcessorNotifier = ServiceProvider.GetRequiredService<ISubscriptionProcessorNotifier>();

            var subscriptionProcessor = new SubscriptionProcessor(ServiceProvider, mockHubClient.Object, subscriptionProcessorNotifier, mockLogger.Object);

            //Act
            subscriptionProcessor.Start = true;            

            await repository.AddAsync(new Entities.EventStream
            {
                Name = streamName,
                StreamId = streamId
            });

            var stream = await repository.GetStreamAsync(streamName);

            await repository.AddAsync(new EventStreamSubscriber
            {
                ConnectionId = "asdjklfkafjfwsxx",
                StreamId = stream.Id,
                SubscriberId = subscriberId,
                SubscriberKey = subscriberKey
            });

            await repository.AddAsync(new EventStreamSubscriber
            {
                ConnectionId = "uqweruiwpqoerffs",
                StreamId = stream.Id,
                SubscriberId = subscriberId1,
                SubscriberKey = subscriberKey1
            });

            await repository.AddAsync(new Event
            {
                StreamId = stream.Id,
                EventId = eventId,
                Type = streamType,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"1\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            }, 
            new Event
            {
                StreamId = stream.Id,
                EventId = eventId,
                Type = streamType,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"2\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            });

            Thread.Sleep(100);

            //Assert
            mockHubClient.Verify(x => x.SendAsync(It.IsAny<EventStreamSubscriberModelResult>()), Times.Exactly(1));

            mockLogger.Verify(m => m.Log(LogLevel.Information, 1000, $"Finished streaming events (2) to subscribers (2) of stream {streamName}.", null, null), Times.Once);

            await repository.AddAsync(new Entities.Event
            {
                StreamId = stream.Id,
                EventId = eventId1,
                Type = streamType,
                Data = Encoding.UTF8.GetBytes("{\"a\":\"2\"}"),
                MetaData = Encoding.UTF8.GetBytes("{}"),
                IsJson = false
            });

            //Assert
            mockHubClient.Verify(x => x.SendAsync(It.IsAny<EventStreamSubscriberModelResult>()), Times.Exactly(2));

            mockLogger.Verify(m => m.Log(LogLevel.Information, 1000, $"Finished streaming events (1) to subscribers (2) of stream {streamName}.", null, null), Times.Once);

            mockLogger.VerifyAll();

            subscriptionProcessor.Start = false;
        }
    }
}
