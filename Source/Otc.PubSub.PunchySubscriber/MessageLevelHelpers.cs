using System;
using System.Text.RegularExpressions;

namespace Otc.PubSub.PunchySubscriber
{
    internal static class MessageLevelHelpers
    {
        public const string BadMessageTopicNameSuffix = "_deadletter_";
        private static readonly string BadMessageTopicSuffixPattern = $"{BadMessageTopicNameSuffix}([0-9])+$";

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

        public static string BuildBadMessageTopicName(string topic, int level)
        {
            string badMessageTopic;

            if (level > 0)
            {
                badMessageTopic = Regex.Replace(topic, BadMessageTopicSuffixPattern, $"{BadMessageTopicNameSuffix}{level}");
            }
            else
            {
                badMessageTopic = $"{topic}{BadMessageTopicNameSuffix}{level}";
            }

            return badMessageTopic;
        }

        public static bool IsBadMessageTopic(string topic)
        {
            return ExtractBadMessageLevel(topic) != -1;
        }
    }
}
