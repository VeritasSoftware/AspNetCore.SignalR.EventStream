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
    public enum DatabaseTypeOptions
    {
        Sqlite,
        SqlServer,
        CosmosDb
    }

    public class EventStreamOptions
    {
        public DatabaseTypeOptions DatabaseType { get; set; }
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
                services.AddScoped(typeof(IRepository), _options.Repository);
            }
            else if (_options.DatabaseType == DatabaseTypeOptions.CosmosDb)
            {
                services.AddScoped<IRepository, CosmosDbRepository>();
                services.AddDbContext<CosmosDbContext>(o => o.UseCosmos(_options.ConnectionString, "EventStream"));
            }
            else if (_options.DatabaseType == DatabaseTypeOptions.SqlServer)
            {
                services.AddScoped<IRepository, SqlServerRepository>();
                services.AddDbContext<SqlServerDbContext>(o => o.UseSqlServer(_options.ConnectionString));
            }
            else
            {
                services.AddScoped<IRepository, SqliteRepository>();
                services.AddDbContext<SqliteDbContext>(ServiceLifetime.Transient);
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
            var config = app.ApplicationServices.GetService<IConfiguration>();
            var secretKey = config["EventStreamSecretKey"];

            if (_options.UseMyRepository && _options.Repository != null)
            {
                var repository = app.ApplicationServices.GetServiceOrNull<IRepository>();
                var repository1 = app.ApplicationServices.GetServiceOrNull<IRepository>();

                if (_options.DeleteDatabaseIfExists)
                    repository.EnsureDatabaseDeleted();
                repository.EnsureDatabaseCreated();

                repository.DeleteAllSubscriptionsAsync().ConfigureAwait(false);

                var logger = app.ApplicationServices.GetServiceOrNull<ILogger<SubscriptionProcessor>>();
                var logger1 = app.ApplicationServices.GetServiceOrNull<ILogger<EventStreamProcessor>>();

                _subscriptionProcessor = new SubscriptionProcessor(repository, _options.EventStreamHubUrl, secretKey, logger)
                {
                    Start = true
                };

                _subscriptionProcessor.Process();

                _eventStreamProcessor = new EventStreamProcessor(repository1, logger1)
                {
                    Start = true
                };

                _eventStreamProcessor.Process();
            }
            else if (_options.DatabaseType == DatabaseTypeOptions.CosmosDb)
            {
                var options = new DbContextOptionsBuilder().UseCosmos(_options.ConnectionString, "EventStream").Options;

                var context = new CosmosDbContext(options);
                var context1 = new CosmosDbContext(options);
                var repository = new CosmosDbRepository(context);
                var repository1 = new CosmosDbRepository(context1);

                if (_options.DeleteDatabaseIfExists)
                    repository.EnsureDatabaseDeleted();
                repository.EnsureDatabaseCreated();

                repository.DeleteAllSubscriptionsAsync().ConfigureAwait(false);

                var logger = app.ApplicationServices.GetServiceOrNull<ILogger<SubscriptionProcessor>>();
                var logger1 = app.ApplicationServices.GetServiceOrNull<ILogger<EventStreamProcessor>>();

                _subscriptionProcessor = new SubscriptionProcessor(repository, _options.EventStreamHubUrl, secretKey, logger)
                {
                    Start = true
                };

                _subscriptionProcessor.Process();

                _eventStreamProcessor = new EventStreamProcessor(repository1, logger1)
                {
                    Start = true
                };

                _eventStreamProcessor.Process();
            }
            else if (_options.DatabaseType == DatabaseTypeOptions.SqlServer)
            {
                var options = new DbContextOptionsBuilder().UseSqlServer(_options.ConnectionString).Options;

                var context = new SqlServerDbContext(options);
                var context1 = new SqlServerDbContext(options);
                var repository = new SqlServerRepository(context);
                var repository1 = new SqlServerRepository(context1);

                if (_options.DeleteDatabaseIfExists)
                    repository.EnsureDatabaseDeleted();
                repository.EnsureDatabaseCreated();                

                repository.DeleteAllSubscriptionsAsync().ConfigureAwait(false);

                var logger = app.ApplicationServices.GetServiceOrNull<ILogger<SubscriptionProcessor>>();
                var logger1 = app.ApplicationServices.GetServiceOrNull<ILogger<EventStreamProcessor>>();

                _subscriptionProcessor = new SubscriptionProcessor(repository, _options.EventStreamHubUrl, secretKey, logger)
                {
                    Start = true
                };

                _subscriptionProcessor.Process();

                _eventStreamProcessor = new EventStreamProcessor(repository1, logger1)
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

                if (_options.DeleteDatabaseIfExists)
                    repository.EnsureDatabaseDeleted();
                repository.EnsureDatabaseCreated();

                repository.DeleteAllSubscriptionsAsync().ConfigureAwait(false);

                var logger = app.ApplicationServices.GetServiceOrNull<ILogger<SubscriptionProcessor>>();
                var logger1 = app.ApplicationServices.GetServiceOrNull<ILogger<EventStreamProcessor>>();

                _subscriptionProcessor = new SubscriptionProcessor(repository, _options.EventStreamHubUrl, secretKey, logger)
                {
                    Start = true
                };

                _subscriptionProcessor.Process();

                _eventStreamProcessor = new EventStreamProcessor(repository1, logger1)
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
