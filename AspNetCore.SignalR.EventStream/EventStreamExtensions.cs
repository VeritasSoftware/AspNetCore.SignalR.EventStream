using AspNetCore.SignalR.EventStream.Authorization;
using AspNetCore.SignalR.EventStream.HubFilters;
using AspNetCore.SignalR.EventStream.Processors;
using AspNetCore.SignalR.EventStream.Repositories;
using AspNetCore.SignalR.EventStream.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
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
        private static SubscriptionProcessor _subscriptionProcessor = null;
        private static EventStreamProcessor _eventStreamProcessor = null;
        private static EventStreamOptions _options = null;

        public static IServiceCollection AddEventStreamHubAuthorization(this IServiceCollection services, Action<EventStreamHubAuthorizationBuilder> action)
        {
            services.AddAuthorization(options => action(new EventStreamHubAuthorizationBuilder(options)));

            return services;
        }

        public static IServiceCollection AddEventStream(this IServiceCollection services, Action<EventStreamOptions> getOptions = null)
        {            
            _options = new EventStreamOptions();

            if (getOptions != null)
            {
                getOptions(_options);
            }
            
            services.AddScoped<IEventStreamService, EventStreamService>();

            if (_options.UseSqlServer)
            {
                services.AddScoped<IRepository, SqlServerRepository>();
                services.AddEntityFrameworkSqlServer().AddDbContext<SqlServerDbContext>(o => o.UseSqlServer(_options.SqlServerConnectionString));
            }
            else
            {
                services.AddScoped<IRepository, SqliteRepository>();
                services.AddEntityFrameworkSqlite().AddDbContext<SqliteDbContext>(ServiceLifetime.Transient);
            }

            services.AddScoped<EventStreamAuthorizeAttribute>();

            services.AddScoped<EventStreamHubFilterAttribute>();

            services.AddSignalR(hubOptions =>
            {
                hubOptions.AddFilter<EventStreamHubFilterAttribute>();
            });

            services.AddScoped<IAuthorizationHandler, AllowAnonymousAuthorizationRequirement>();

            return services;
        }

        public static IApplicationBuilder UseEventStream(this IApplicationBuilder app)
        {            
            if (_options.UseSqlServer)
            {
                var options = new DbContextOptionsBuilder().UseSqlServer(_options.SqlServerConnectionString).Options;

                var context = new SqlServerDbContext(options);
                var context1 = new SqlServerDbContext(options);
                var repository = new SqlServerRepository(context);
                var repository1 = new SqlServerRepository(context1);

                //context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                context.Database.Migrate();

                repository.DeleteAllSubscriptionsAsync().ConfigureAwait(false);

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
                var repository = new SqliteRepository(context);
                var repository1 = new SqliteRepository(context1);

                //context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
                context.Database.Migrate();

                repository.DeleteAllSubscriptionsAsync().ConfigureAwait(false);

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
