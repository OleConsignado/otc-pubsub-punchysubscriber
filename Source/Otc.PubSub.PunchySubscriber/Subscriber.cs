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
        private readonly IPubSub pubSub;
        private readonly SubscriberConfiguration configuration;
        private readonly ILogger logger;

        public Subscriber(IPubSub pubSub, ILoggerFactory loggerFactory, SubscriberConfiguration subscriberConfiguration)
        {
            this.pubSub = pubSub ?? throw new ArgumentNullException(nameof(pubSub));
            this.configuration = subscriberConfiguration ?? throw new ArgumentNullException(nameof(subscriberConfiguration));
            logger = loggerFactory?.CreateLogger<Subscriber>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task SubscribeAsync(Func<PunchyMessage, Task> onMessageAsync, string groupId, CancellationToken cancellationToken, params string[] topics)
        {
            if (onMessageAsync == null)
            {
                throw new ArgumentNullException(nameof(onMessageAsync));
            }

            if (groupId == null)
            {
                throw new ArgumentNullException(nameof(groupId));
            }

            if(!TopicNameHelpers.IsValid(groupId))
            {
                throw new ArgumentException(
                    $"The groupId must match regex pattern: '{TopicNameHelpers.TopicOrGroupIdValidationRegexPattern}'", nameof(groupId));
            }

            var messageHandler = new MessageHandler(onMessageAsync, pubSub, logger, configuration, groupId);
            var retryerTopics = new List<string>();
            
            foreach (var topic in topics)
            {
                if (!TopicNameHelpers.IsValid(topic))
                {
                    throw new ArgumentException(
                        $"The topic name must match regex pattern: '{TopicNameHelpers.TopicOrGroupIdValidationRegexPattern}'. " +
                        $"Validation failed for '{topic}'.", nameof(topics));
                }

                if (!TopicNameHelpers.IsBadMessageTopic(topic))
                {
                    for (int i = 0; i < configuration.LevelDelaysInSeconds.Length; i++)
                    {
                        retryerTopics.Add(TopicNameHelpers.BuildBadMessageTopicName(topic, i, groupId));
                    }
                }
            }

            await pubSub.SubscribeAsync(messageHandler, groupId, cancellationToken, topics.Union(retryerTopics).ToArray());
        }
    }
}
