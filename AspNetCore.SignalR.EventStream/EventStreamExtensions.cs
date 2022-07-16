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
