using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Otc.PubSub.Abstractions;
using Otc.PubSub.Kafka;
using Otc.PubSub.PunchySubscriber.Abstractions;
using System;
using System.Text;
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
                    LevelDelaysInSeconds = new int[] { 30, 45, 90, 180, 300, 900, 1800, 7200, 36000 }
                });
            });

            services.AddKafkaPubSub(config =>
            {
                config.Configure(new KafkaPubSubConfiguration()
                {
                    BrokerList = "192.168.145.100"
                });
            });

            services.AddLogging(c =>
            {
                c.SetMinimumLevel(LogLevel.Debug);
                c.AddDebug();
            });
            
            serviceProvider = services.BuildServiceProvider();
        }

        private string topicName;
        private string groupId;

        private const int MessagesToGenerate = 5;

        private async Task Populate()
        {
            var rand = new Random();
            var suffix = rand.Next(9999).ToString();

            topicName = $"tp_{suffix}";
            groupId = $"g_{suffix}";

            var pubSub = serviceProvider.GetService<IPubSub>();

            for (int i = 0; i < MessagesToGenerate; i++)
            {
                await pubSub.PublishAsync(topicName, Encoding.UTF8.GetBytes($"Message {i}"));
            }
        }

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        [Fact]
        public async Task Test_Subscriber()
        {
            await Populate();

            var subscriber = serviceProvider.GetService<ISubscriber>();

            var deadLetterSubscriberTask = subscriber.SubscribeToDeadLetterAsync(OnDeadLetterAsync, groupId, cancellationTokenSource.Token, topicName);

            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            {
                await subscriber.SubscribeAsync(OnMessageAsync, groupId, cancellationTokenSource.Token, topicName);
            });

            Assert.Equal(MessagesToGenerate, deadMessageCount);
        }

        private int deadMessageCount = 0;

        private async Task OnDeadLetterAsync(PunchyMessage arg)
        {
            deadMessageCount++;

            if(deadMessageCount >= MessagesToGenerate)
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(10000);
                    cancellationTokenSource.Cancel();
                }
            }
        }

        private Task OnMessageAsync(PunchyMessage message)
        {
            throw new Exception("bla");
        }
    }
}
