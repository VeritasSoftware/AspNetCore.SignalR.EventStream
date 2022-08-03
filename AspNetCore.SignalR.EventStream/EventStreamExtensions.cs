using AspNetCore.SignalR.EventStream.Repositories;
using AspNetCore.SignalR.EventStream.Services;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream
{
    public class EventStreamOptions
    {
        public bool UseSqlServer { get; set; }
        public string? SqlServerConnectionString { get; set; }
        public string EventStreamHubUrl { get; set; }
    }

    public static class EventStreamExtensions
    {
        private static SubscriptionProcessor _subscriptionProcessor;
        private static EventStreamProcessor _eventStreamProcessor;
        private static EventStreamOptions _options;

        public static IServiceCollection AddEventStream(this IServiceCollection services, Action<EventStreamOptions> getOptions = null)
        {
            services.AddScoped<IRepository, Repository>();
            services.AddScoped<IEventStreamService, EventStreamService>();

            _options = new EventStreamOptions();

            if (getOptions != null)
            {
                getOptions(_options);
            }
            
            if (_options.UseSqlServer)
            {
                services.AddEntityFrameworkSqlServer().AddDbContext<IDbContext, SqlServerDbContext>(o => o.UseSqlServer(_options.SqlServerConnectionString));
            }
            else
            {
                services.AddEntityFrameworkSqlite().AddDbContext<IDbContext, SqliteDbContext>(ServiceLifetime.Transient);
            }            

            return services;
        }

        public static IApplicationBuilder UseEventStream(this IApplicationBuilder app)
        {
            Repository repository;
            Repository repository1;

            if (_options.UseSqlServer)
            {
                var options = new DbContextOptionsBuilder().UseSqlServer(_options.SqlServerConnectionString).Options;

                var context = new SqlServerDbContext(options);
                var context1 = new SqlServerDbContext(options);
                repository = new Repository(context);
                repository1 = new Repository(context1);

                //context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                context.Database.Migrate();                

                _subscriptionProcessor = new SubscriptionProcessor(repository, _options.EventStreamHubUrl)
                {
                    Start = true
                };

                _subscriptionProcessor.Process();

                _eventStreamProcessor = new EventStreamProcessor(repository1)
                {
                    Start = true
                };

                _eventStreamProcessor.Process();                  
            }
            else
            {
                var context = app.ApplicationServices.GetRequiredService<SqliteDbContext>();
                var context1 = app.ApplicationServices.GetRequiredService<SqliteDbContext>();
                repository = new Repository(context);
                repository1 = new Repository(context1);

                //context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                context.Database.Migrate();

                _subscriptionProcessor = new SubscriptionProcessor(repository, _options.EventStreamHubUrl)
                {
                    Start = true
                };

                _subscriptionProcessor.Process();

                _eventStreamProcessor = new EventStreamProcessor(repository1)
                {
                    Start = true
                };

                _eventStreamProcessor.Process();
            }                                               

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
