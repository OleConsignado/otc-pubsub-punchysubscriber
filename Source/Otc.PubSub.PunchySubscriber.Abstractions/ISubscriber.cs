using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Otc.PubSub.PunchySubscriber.Abstractions
{
    public interface ISubscriber
    {
        Task SubscribeAsync(Func<PunchyMessage, Task> onMessageAsync, string groupId, CancellationToken cancellationToken, params string[] topics);
        Task SubscribeToDeadLetterAsync(Func<PunchyMessage, Task> onMessageAsync, string groupId, CancellationToken cancellationToken, params string[] topics);
    }
}
