using Newtonsoft.Json;

namespace AspNetCore.SignalR.EventStream.Server
{
    public class Startup
    {        
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

            //Add EventStream
            services.AddEventStream();
        }

        public void Configure(IApplicationBuilder app)
        {
            //Use Event Stream
            app.UseEventStream(options => options.EventStreamHubUrl = "https://localhost:5001/eventstreamhub");

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                //EventStreamHub endpoint
                endpoints.MapHub<EventStreamHub>("/eventstreamhub");
            });
        }
    }
}
