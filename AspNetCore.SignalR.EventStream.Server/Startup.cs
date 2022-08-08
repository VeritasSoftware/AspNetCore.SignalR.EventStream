using AspNetCore.SignalR.EventStream.Authorization;
using AspNetCore.SignalR.EventStream.HubFilters;
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
            //Event Stream SignalR Hub Filter
            services.AddScoped<IEventStreamHubFilter, HubFilterService>();

            //Event Stream Admin Http endpoints security
            services.AddScoped<IEventStreamAuthorization, AuthorizationService>();

            //Event Stream SignalR Hub security
            services.AddAuthentication();

            //Set up your Authorization policy requirements here, for the Event Stream SignalR Hub
            //If you want anonymous access, use below AllowAnonymousAuthorizationRequirement
            services.AddAuthorization(builder => builder.AddHubPolicyRequirements(new AllowAnonymousAuthorizationRequirement())
                                                        .AddHubPublishPolicyRequirements(new AllowAnonymousAuthorizationRequirement())
                                                        .AddHubSubscribePolicyRequirements(new AllowAnonymousAuthorizationRequirement())
                                                        .AddHubUnsubscribePolicyRequirements(new AllowAnonymousAuthorizationRequirement()));

            //Set up CORS as you want
            services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
            {
                builder
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowAnyOrigin();
            }));

            //Hook up Event Stream Hub using SignalR
            services.AddSignalR().AddNewtonsoftJsonProtocol(o =>
            {
                o.PayloadSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            //Add Event Stream
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

            //Use security
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                //Event Stream Hub endpoint
                endpoints.MapHub<EventStreamHub>("/eventstreamhub");
                endpoints.MapControllers();
            });
        }
    }
}
