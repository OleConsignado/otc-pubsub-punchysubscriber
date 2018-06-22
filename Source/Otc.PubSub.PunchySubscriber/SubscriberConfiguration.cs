namespace Otc.PubSub.PunchySubscriber
{
    public class SubscriberConfiguration
    {
        public int[] LevelDelaysInSeconds { get; set; } = new int[]
        {
            300,
            600,
            900
        };
    }
}
