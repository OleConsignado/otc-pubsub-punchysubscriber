using System.ComponentModel.DataAnnotations;

namespace Otc.PubSub.PunchySubscriber
{
    public class SubscriberConfiguration
    {
        [Required]
        public int[] LevelDelaysInSeconds { get; set; }

        public bool SubscribeToRetryerTopics { get; set; } = true;
    }
}
