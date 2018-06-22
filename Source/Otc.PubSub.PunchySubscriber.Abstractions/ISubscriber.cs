using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.PubSub.PunchySubscriber.Abstractions
{
    public interface ISubscriber
    {
        Task SubscribeAsync(Action<PunchyMessage> action, string group, CancellationToken cancellationToken, params string[] topics);
    }
}
