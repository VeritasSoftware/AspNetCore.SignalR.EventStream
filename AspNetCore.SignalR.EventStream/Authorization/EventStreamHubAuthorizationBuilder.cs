using Microsoft.AspNetCore.Authorization;

namespace AspNetCore.SignalR.EventStream.Authorization
{
    public class EventStreamHubAuthorizationBuilder
    {
        private readonly AuthorizationOptions _options;

        public EventStreamHubAuthorizationBuilder(AuthorizationOptions options)
        {
            _options = options;
        }

        public EventStreamHubAuthorizationBuilder AddHubAuthorizationPolicy(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHub", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubPublishAuthorizationPolicy(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubPublish", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubSubscribeAuthorizationPolicy(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubSubscribe", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubUnsubscribeAuthorizationPolicy(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubUnsubscribe", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }
    }
}
