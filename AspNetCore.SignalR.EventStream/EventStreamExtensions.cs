using AspNetCore.SignalR.EventStream.Authorization;
using AspNetCore.SignalR.EventStream.Clients;
using AspNetCore.SignalR.EventStream.HubFilters;
using AspNetCore.SignalR.EventStream.Processors;
using AspNetCore.SignalR.EventStream.Repositories;
using AspNetCore.SignalR.EventStream.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream
{
    public enum DatabaseTypeOptions
    {
        Sqlite,
        SqlServer,
        CosmosDb
    }

    public class EventStreamOptions
    {
        public DatabaseTypeOptions DatabaseType { get; set; } = DatabaseTypeOptions.Sqlite;
        public string? ConnectionString { get; set; }
        public bool DeleteDatabaseIfExists { get; set; } = false;
        public string EventStreamHubUrl { get; set; }
        public bool UseMyRepository { get; set; } = false;
        public bool RegisterMyRepository { get; set; } = true;
        public Type? Repository { get; set; }
    }

    public static class EventStreamExtensions
    {
        private static SubscriptionProcessor _subscriptionProcessor = null;
        private static EventStreamProcessor _eventStreamProcessor = null;
        private static EventStreamOptions _options = null;

        public static IServiceCollection AddAuthorization(this IServiceCollection services, Action<EventStreamHubAuthorizationBuilder> action)
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
            
            if (_options.UseMyRepository && _options.RegisterMyRepository && _options.Repository != null)
            {
                services.AddTransient(typeof(IRepository), _options.Repository);
            }
            else if (_options.DatabaseType == DatabaseTypeOptions.CosmosDb)
            {
                services.AddTransient<IRepository, CosmosDbRepository>();
                services.AddDbContext<CosmosDbContext>(o => o.UseCosmos(_options.ConnectionString, "EventStream"), ServiceLifetime.Transient, ServiceLifetime.Transient);
            }
            else if (_options.DatabaseType == DatabaseTypeOptions.SqlServer)
            {
                services.AddTransient<IRepository, SqlServerRepository>();
                services.AddDbContext<SqlServerDbContext>(o => o.UseSqlServer(_options.ConnectionString), ServiceLifetime.Transient, ServiceLifetime.Transient);
            }
            else
            {
                services.AddTransient<IRepository, SqliteRepository>();
                services.AddDbContext<SqliteDbContext>(o => o.UseSqlite(_options.ConnectionString), ServiceLifetime.Transient, ServiceLifetime.Transient);
            }

            services.AddScoped<EventStreamAuthorizeAttribute>();

            services.AddScoped<EventStreamHubFilterAttribute>();

            services.AddSignalR(hubOptions =>
            {
                hubOptions.AddFilter<EventStreamHubFilterAttribute>();
            });

            services.AddScoped<IAuthorizationHandler, AllowAnonymousAuthorizationRequirement>();

            services.AddSingleton<IEventStreamHubClient>(o => new EventStreamHubClient(_options.EventStreamHubUrl,
                                                                o.GetServiceOrNull<IConfiguration>()["EventStreamSecretKey"],
                                                                o.GetService<ILogger<EventStreamHubClient>>()));

            return services;
        }

        public static IApplicationBuilder UseEventStream(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetServiceOrNull<ILogger<SubscriptionProcessor>>();
            var logger1 = app.ApplicationServices.GetServiceOrNull<ILogger<EventStreamProcessor>>();

            var repository = app.ApplicationServices.GetServiceOrNull<IRepository>();
            var repository1 = app.ApplicationServices.GetServiceOrNull<IRepository>();
            var repository2 = app.ApplicationServices.GetServiceOrNull<IRepository>();

            var applicationLifeTime = app.ApplicationServices.GetServiceOrNull<IHostApplicationLifetime>();

            applicationLifeTime.ApplicationStopping.Register(async () =>
            {
                try
                {
                    logger.LogInformation("Deleting all subscriptions from database.");

                    await repository2.DeleteAllSubscriptionsAsync();

                    logger.LogInformation("Finished deleting all subscriptions from database.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Deleting all subscriptions from database failed.");
                }

                logger.LogInformation("Event Stream Server shut down.");
            });

            applicationLifeTime.ApplicationStarted.Register(async () =>
            {               
                logger.LogInformation("Event Stream Server started.");
            });

            if (_options.DeleteDatabaseIfExists)
                repository.EnsureDatabaseDeleted();
            repository.EnsureDatabaseCreated();

            logger.LogInformation("Deleting all subscriptions from database.");
            repository.DeleteAllSubscriptionsAsync().ConfigureAwait(false);
            logger.LogInformation("Finished deleting all subscriptions from database.");

            var eventStreamHubClient = app.ApplicationServices.GetServiceOrNull<IEventStreamHubClient>();

            _subscriptionProcessor = new SubscriptionProcessor(repository, eventStreamHubClient, logger)
            {
                Start = true
            };

            _subscriptionProcessor.Process();

            _eventStreamProcessor = new EventStreamProcessor(repository1, logger1)
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
