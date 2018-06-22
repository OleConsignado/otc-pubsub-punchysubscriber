using Microsoft.Extensions.Logging;
using Otc.PubSub.Abstractions;
using Otc.PubSub.PunchySubscriber.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.PubSub.PunchySubscriber
{
    public class Subscriber : ISubscriber
    {
        internal const string BadMessageTopicNameSuffix = "_deadletter_";

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
            var retryerTopics = new List<string>();

            if (configuration.SubscribeToRetryerTopics)
            {
                foreach (var topic in topics)
                {
                    for (int i = 0; i < configuration.LevelDelaysInSeconds.Length - 1; i++)
                    {
                        retryerTopics.Add($"{topic}{BadMessageTopicNameSuffix}{i}");
                    }
                }
            }

            await pubSub.SubscribeAsync(messageHandler, group, cancellationToken, topics.Union(retryerTopics).ToArray());
        }
    }
}
