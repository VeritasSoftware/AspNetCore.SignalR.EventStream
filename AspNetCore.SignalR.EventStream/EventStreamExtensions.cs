using AspNetCore.SignalR.EventStream.Repositories;
using AspNetCore.SignalR.EventStream.Services;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream
{
    public class EventStreamOptions
    {
        public string EventStreamHubUrl { get; set; }
    }

    public static class EventStreamExtensions
    {
        private static SubscriptionProcessor _subscriptionProcessor;
        private static EventStreamProcessor _eventStreamProcessor;

        public static IServiceCollection AddEventStream(this IServiceCollection services)
        {
            services.AddScoped<IRepository, SqliteRepository>();
            services.AddScoped<IEventStreamService, EventStreamService>();
            services.AddEntityFrameworkSqlite().AddDbContext<SqliteDbContext>(ServiceLifetime.Transient);

            return services;
        }

        public static IApplicationBuilder UseEventStream(this IApplicationBuilder app, Action<EventStreamOptions> getOptions)
        {
            var context = app.ApplicationServices.GetRequiredService<SqliteDbContext>();
            var context1 = app.ApplicationServices.GetRequiredService<SqliteDbContext>();
            var repository = new SqliteRepository(context);
            var repository1 = new SqliteRepository(context1);

            //context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.Database.Migrate();

            var options = new EventStreamOptions();
            getOptions(options);

            _subscriptionProcessor = new SubscriptionProcessor(repository, options.EventStreamHubUrl)
            {
                Start = true
            };

            _subscriptionProcessor.Process();

            _eventStreamProcessor = new EventStreamProcessor(repository1)
            {
                Start = true
            };

            _eventStreamProcessor.Process();            

            return app;
        }

        internal static T GetServiceOrNull<T>(this IServiceProvider serviceProvider)
            where T : class
        {
            try
            {
                return serviceProvider.GetService<T>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
