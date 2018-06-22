using Microsoft.Extensions.DependencyInjection;
using Otc.PubSub.Kafka;
using Otc.PubSub.PunchySubscriber.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Otc.PubSub.PunchySubscriber.Tests
{
    public class IntegrationsTests
    {
        private readonly IServiceProvider serviceProvider;

        public IntegrationsTests()
        {
            var services = new ServiceCollection();
            services.AddPunchySubscriber(config =>
            {
                config.Configure(new SubscriberConfiguration()
                {
                    LevelDelaysInSeconds = new int [] { 30, 60, 90 }
                });
            });

            services.AddKafkaPubSub(config =>
            {
                config.Configure(new KafkaPubSubConfiguration()
                {
                    BrokerList = "192.168.145.100"
                });
            });

            services.AddLogging();
            serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task Test_Subscriber()
        {
            var subscriber = serviceProvider.GetService<ISubscriber>();
            var cts = new CancellationTokenSource();

            await subscriber.SubscribeAsync(OnMessage, "xeydr", cts.Token, "teste");
        }

        private void OnMessage(PunchyMessage message)
        {
            throw new Exception("bla");
        }
    }
}
