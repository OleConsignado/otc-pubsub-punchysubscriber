using Otc.PubSub.PunchySubscriber;
using Otc.PubSub.PunchySubscriber.Abstractions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class OtcPunchySubscriberServiceCollectionExtensions
    {
        public static IServiceCollection AddPunchySubscriber(this IServiceCollection services, Action<PunchySubscriberConfigurationLambda> config = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddScoped<ISubscriber, Subscriber>();

            var consiguration = new PunchySubscriberConfigurationLambda(services);

            if (config == null)
            {
                services.AddSingleton(new SubscriberConfiguration());
            }
            else
            {
                config.Invoke(consiguration);
            }

            return services;
        }
    }

    public class PunchySubscriberConfigurationLambda
    {
        private readonly IServiceCollection services;

        public PunchySubscriberConfigurationLambda(IServiceCollection services)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public void Configure(SubscriberConfiguration subscriberConfiguration)
        {
            services.AddSingleton(subscriberConfiguration);
        }
    }
}
