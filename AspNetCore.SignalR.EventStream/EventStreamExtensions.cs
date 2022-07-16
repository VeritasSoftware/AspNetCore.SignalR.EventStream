using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.SignalR.EventStream
{
    public static class EventStreamExtensions
    {
        public static IServiceCollection AddEventStream(this IServiceCollection services)
        {
            services.AddScoped<IRepository, SqliteRepository>();
            services.AddEntityFrameworkSqlite().AddDbContext<SqliteDbContext>(ServiceLifetime.Transient);

            return services;
        }
    }
}
