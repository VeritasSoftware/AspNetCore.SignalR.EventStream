using Newtonsoft.Json;

namespace AspNetCore.SignalR.EventStream.Server
{
    public class Startup
    {
        private SubscriptionProcessor _processor;        

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Hook up EventStreamHub using SignalR
            services.AddSignalR().AddNewtonsoftJsonProtocol(o =>
            {
                o.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            services.AddEventStream();
        }

        public void Configure(IApplicationBuilder app)
        {
            var context = app.ApplicationServices.GetRequiredService<SqliteDbContext>();
            //var repository = app.ApplicationServices.GetRequiredService<IRepository>();
            var repository = new SqliteRepository(context);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            _processor = new SubscriptionProcessor(repository, "https://localhost:5001/eventstreamhub")
            {
                Start = true
            };

            _processor.Process();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                //GatewayHub endpoint
                endpoints.MapHub<EventStreamHub>("/eventstreamhub");
            });
        }
    }
}
