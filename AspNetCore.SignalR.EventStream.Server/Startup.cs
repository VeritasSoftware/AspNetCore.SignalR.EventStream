using AspNetCore.SignalR.EventStream.Hubs;
using Microsoft.OpenApi.Models;
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
            services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowAnyOrigin();
            }));

            //Hook up EventStreamHub using SignalR
            services.AddSignalR().AddNewtonsoftJsonProtocol(o =>
            {
                o.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            //Add EventStream
            services.AddEventStream(options => 
            {
                options.UseSqlServer = false;
                options.SqlServerConnectionString = Configuration.GetConnectionString("EventStreamDatabase");
                options.EventStreamHubUrl = "https://localhost:5001/eventstreamhub";
            });

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My Event Stream Server", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            //Use Event Stream
            app.UseEventStream();

            app.UseCors("CorsPolicy");

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My Event Stream Server");
            });

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                //EventStreamHub endpoint
                endpoints.MapHub<EventStreamHub>("/eventstreamhub");
                endpoints.MapControllers();
            });
        }
    }
}
