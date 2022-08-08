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

        public EventStreamHubAuthorizationBuilder AddHubPolicyRequirements(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubPolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubPublishPolicyRequirements(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubPublishPolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubSubscribePolicyRequirements(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubSubscribePolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }

        public EventStreamHubAuthorizationBuilder AddHubUnsubscribePolicyRequirements(params IAuthorizationRequirement[] requirements)
        {
            _options.AddPolicy("EventStreamHubUnsubscribePolicy", policy =>
            {
                policy.AddRequirements(requirements);
            });

            return this;
        }
    }
}
