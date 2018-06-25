using System;
using System.Text.RegularExpressions;

namespace Otc.PubSub.PunchySubscriber
{
    internal static class TopicNameHelpers
    {
        private const string BadMessageTopicNameSuffix = "retryer";
        private const string DeadLetterTopicNameSuffix = "deadletter";
        private static string BadMessageTopicSuffixPattern => $"_[A-Za-z0-9._-]+_{BadMessageTopicNameSuffix}_([0-9])+$";

        public static int ExtractBadMessageNextLevel(string topic)
        {
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

        public static string BuildBadMessageTopicName(string topic, int level, string groupId)
        {
            string badMessageTopic;

            var topicNameSuffixWithGroupIdAndLevel = $"_{groupId}_{BadMessageTopicNameSuffix}_{level}";

            if (IsBadMessageTopic(topic))
            {
                badMessageTopic = Regex.Replace(topic, BadMessageTopicSuffixPattern, topicNameSuffixWithGroupIdAndLevel);
            }
            else
            {
                badMessageTopic = $"{topic}{topicNameSuffixWithGroupIdAndLevel}";
            }

            return badMessageTopic;
        }

        public static bool IsBadMessageTopic(string topic)
        {
            return ExtractBadMessageLevel(topic) != -1;
        }

        public const string TopicOrGroupIdValidationRegexPattern = "^[A-Za-z0-9._-]{1,96}$";

        public static bool IsValid(string topicOrGroupId)
        {
            return Regex.IsMatch(topicOrGroupId, TopicOrGroupIdValidationRegexPattern);
        }

        public static string BuildDeadLetterTopicName(string topic, string groupId)
        {
            string deadLetterTopic;

            var deadLetterTopicNameSuffixWithGroupId = $"_{groupId}_{DeadLetterTopicNameSuffix}";

            if (IsBadMessageTopic(topic))
            {
                deadLetterTopic = Regex.Replace(topic, BadMessageTopicSuffixPattern, deadLetterTopicNameSuffixWithGroupId);
            }
            else
            {
                deadLetterTopic = $"{topic}{deadLetterTopicNameSuffixWithGroupId}";
            }

            return deadLetterTopic;
        }
    }
}
