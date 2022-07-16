using Microsoft.EntityFrameworkCore;

namespace AspNetCore.SignalR.EventStream
{
    public class EventStreamOptions
    {
        public string EventStreamHubUrl { get; set; }
    }

    public static class EventStreamExtensions
    {
        private static SubscriptionProcessor _processor;

        public static IServiceCollection AddEventStream(this IServiceCollection services)
        {
            services.AddScoped<IRepository, SqliteRepository>();
            services.AddEntityFrameworkSqlite().AddDbContext<SqliteDbContext>(ServiceLifetime.Transient);

            return services;
        }

        public static IApplicationBuilder UseEventStream(this IApplicationBuilder app, Action<EventStreamOptions> getOptions)
        {
            var context = app.ApplicationServices.GetRequiredService<SqliteDbContext>();
            var repository = new SqliteRepository(context);

            //context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.Database.Migrate();

            var options = new EventStreamOptions();
            getOptions(options);

            _processor = new SubscriptionProcessor(repository, options.EventStreamHubUrl)
            {
                Start = true
            };

            _processor.Process();

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
