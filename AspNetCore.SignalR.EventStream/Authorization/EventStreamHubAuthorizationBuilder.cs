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
            _options.AddPolicy("EventStreamHubPolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubPublishAuthorizationPolicy(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubPublishPolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubSubscribeAuthorizationPolicy(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubSubscribePolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubUnsubscribeAuthorizationPolicy(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubUnsubscribePolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }
    }
}
