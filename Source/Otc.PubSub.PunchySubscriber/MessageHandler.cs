using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Otc.PubSub.Abstractions;
using Otc.PubSub.PunchySubscriber.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Otc.PubSub.PunchySubscriber
{
    public class MessageHandler : IMessageHandler
    {
        private readonly Action<PunchyMessage> action;
        private readonly IPubSub pubSub;
        private readonly ILogger logger;
        private readonly SubscriberConfiguration configuration;
        private readonly int badMessageMaxLevels;

        public MessageHandler(Action<PunchyMessage> action, IPubSub pubSub, ILogger logger,
            SubscriberConfiguration configuration)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.pubSub = pubSub ?? throw new ArgumentNullException(nameof(pubSub));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            badMessageMaxLevels = configuration.LevelDelaysInSeconds.Length;
        }

        public Task OnErrorAsync(object error, IMessage message)
        {
            throw new NotImplementedException();
        }

        private IMessage GetSourceMessage(IMessage message, List<Attempt> attempts)
        {
            var badMessageCurrentLevel = BadMessageLevelHelpers.ExtractBadMessageLevel(message.Topic);

            while (message != null && badMessageCurrentLevel >= 0)
            {
                var badMessageContents = JsonConvert.DeserializeObject<BadMessageContents>(
                    Encoding.UTF8.GetString(message.MessageBytes));

                var attemp = new Attempt()
                {
                    BadMessageContents = badMessageContents,
                    MessageAddress = message.MessageAddress,
                    Topic = message.Topic,
                    Timestamp = message.Timestamp
                };

                attempts.Insert(0, attemp);

                // retrying
                var messageTimestamp = message.Timestamp;

                var messageAge = (int)Math.Round((DateTimeOffset.Now - messageTimestamp).TotalMilliseconds);
                var delayMilliseconds = configuration.LevelDelaysInSeconds[badMessageCurrentLevel] * 1000;

                if (messageAge >= delayMilliseconds)
                {
                    // the message is being replaced here with the original message
                    message = pubSub.ReadSingle(badMessageContents.SourceMessageAddress);

                    badMessageCurrentLevel = BadMessageLevelHelpers.ExtractBadMessageLevel(message.Topic);
                }
                else
                {
                    message = null; // retry time not ellapsed
                }
            }

            return message;
        }

        public async Task OnMessageAsync(IMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var badMessageNextLevel = BadMessageLevelHelpers.ExtractBadMessageNextLevel(message.Topic);
            var badMessageNextLevelTopicName = BadMessageLevelHelpers.BuildBadMessageTopicName(message.Topic, badMessageNextLevel);

            var attempts = new List<Attempt>();
            var badMessageCandidate = message;

            message = GetSourceMessage(badMessageCandidate, attempts);

            // If message is null, retry time not ellapsed so will ignore this message for now. 
            // It will be read again latter

            if (message != null) 
            {
                try
                {
                    action.Invoke(new PunchyMessage()
                    {
                        MessageAddress = message.MessageAddress,
                        MessageBytes = message.MessageBytes,
                        Timestamp = message.Timestamp,
                        Topic = message.Topic,
                        Attempts = attempts
                    });
                }
                catch (Exception exception)
                {
                    if (badMessageNextLevel < badMessageMaxLevels)
                    {
                        logger.LogWarning(exception, "Action failed for message, moving to bad message level '{BadMessageNextLevel}'. " +
                            "Will try again in {RetrySeconds}. Topic: {Topic}; Original message address: {@MessageAddress}",
                            badMessageNextLevel, configuration.LevelDelaysInSeconds[badMessageNextLevel],
                            message.Topic, message.MessageAddress);
                    }
                    else // is final level
                    {
                        logger.LogError(exception, "Action failed for message, will NOT try again. Topic: {Topic}; Original message address: {@MessageAddress}",
                            message.Topic, message.MessageAddress);
                    }

                    var badMessageContents = new BadMessageContents()
                    {
                        Exception = exception,
                        SourceMessageAddress = badMessageCandidate.MessageAddress
                    };

                    await pubSub.PublishAsync(badMessageNextLevelTopicName,
                        Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(badMessageContents)));
                }

                await badMessageCandidate.CommitAsync();
            }
        }

        private static class BadMessageLevelHelpers
        {
            private static readonly string BadMessageTopicSuffixPattern = $"{Subscriber.BadMessageTopicNameSuffix}([0-9])+$";

            public static int ExtractBadMessageNextLevel(string topic)
            {
                //var match = Regex.Match(topic, BadMessageTopicSuffixPattern);
                //int level = 0;

                //if (match.Success)
                //{
                //    level = Convert.ToInt32(match.Groups[1].Value, 10) + 1;
                //}

                //return level;

                return ExtractBadMessageLevel(topic) + 1;
            }

            public static int ExtractBadMessageLevel(string topic)
            {
                var match = Regex.Match(topic, BadMessageTopicSuffixPattern);
                int level = -1;

                if (match.Success)
                {
                    level = Convert.ToInt32(match.Groups[1].Value, 10);
                }

                return level;
            }

            public static string BuildBadMessageTopicName(string topic, int level)
            {
                string badMessageTopic;

                if (level > 0)
                {
                    badMessageTopic = Regex.Replace(topic, BadMessageTopicSuffixPattern, $"{Subscriber.BadMessageTopicNameSuffix}{level}");
                }
                else
                {
                    badMessageTopic = $"{topic}{Subscriber.BadMessageTopicNameSuffix}{level}";
                }

                return badMessageTopic;
            }
        } 
    }
}
