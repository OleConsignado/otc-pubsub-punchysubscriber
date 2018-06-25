using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Otc.PubSub.Abstractions;
using Otc.PubSub.PunchySubscriber.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Otc.PubSub.PunchySubscriber
{
    public class MessageHandler : IMessageHandler
    {
        private readonly Func<PunchyMessage, Task> onMessageAsync;
        private readonly IPubSub pubSub;
        private readonly ILogger logger;
        private readonly SubscriberConfiguration configuration;
        private readonly string groupId;
        private readonly int badMessageMaxLevels;

        public MessageHandler(Func<PunchyMessage, Task> onMessageAsync, IPubSub pubSub, ILogger logger,
            SubscriberConfiguration configuration, string groupId)
        {
            this.onMessageAsync = onMessageAsync ?? throw new ArgumentNullException(nameof(onMessageAsync));
            this.pubSub = pubSub ?? throw new ArgumentNullException(nameof(pubSub));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.groupId = groupId ?? throw new ArgumentNullException(nameof(groupId));
            badMessageMaxLevels = configuration.LevelDelaysInSeconds.Length;
        }

        public async Task OnErrorAsync(object error, IMessage message)
        {
            logger.LogError("Error while reading message with address '{@MessageAddress}'. Error: '{@Error}'", message?.MessageAddress, error);

            await Task.CompletedTask;
        }

        private IMessage GetSourceMessage(IMessage message, List<Attempt> attempts)
        {
            var badMessageCurrentLevel = TopicNameHelpers.ExtractBadMessageLevel(message.Topic);

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

                    badMessageCurrentLevel = TopicNameHelpers.ExtractBadMessageLevel(message.Topic);
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

            var badMessageNextLevel = TopicNameHelpers.ExtractBadMessageNextLevel(message.Topic);

            string badMessageNextLevelTopicName;

            if (badMessageNextLevel == badMessageMaxLevels) 
            {
                badMessageNextLevelTopicName = TopicNameHelpers.BuildDeadLetterTopicName(message.Topic, groupId);
            }
            else
            {
                badMessageNextLevelTopicName = TopicNameHelpers.BuildBadMessageTopicName(message.Topic, badMessageNextLevel, groupId);
            }

            var attempts = new List<Attempt>();
            var badMessageCandidate = message;

            message = GetSourceMessage(badMessageCandidate, attempts);

            // If message is null, retry time not ellapsed so will ignore this message for now. 
            // It will be read again latter

            if (message != null) 
            {
                try
                {
                    await onMessageAsync.Invoke(new PunchyMessage()
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
    }
}
