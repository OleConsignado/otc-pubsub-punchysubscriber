using Microsoft.Extensions.Logging;
using Otc.PubSub.Abstractions;
using Otc.PubSub.PunchySubscriber.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.PubSub.PunchySubscriber
{
    public class Subscriber : ISubscriber
    {
        private readonly IPubSub pubSub;
        private readonly SubscriberConfiguration configuration;
        private readonly ILogger logger;

        public Subscriber(IPubSub pubSub, ILoggerFactory loggerFactory, SubscriberConfiguration subscriberConfiguration)
        {
            this.pubSub = pubSub ?? throw new ArgumentNullException(nameof(pubSub));
            this.configuration = subscriberConfiguration ?? throw new ArgumentNullException(nameof(subscriberConfiguration));
            logger = loggerFactory?.CreateLogger<Subscriber>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task SubscribeAsync(Action<PunchyMessage> action, string group, CancellationToken cancellationToken, params string[] topics)
        {
            var messageHandler = new MessageHandler(action, pubSub, logger, configuration);
            await pubSub.SubscribeAsync(messageHandler, group, cancellationToken, topics);
        }
    }
}
