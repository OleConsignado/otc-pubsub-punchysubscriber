using System;
using System.Collections.Generic;

namespace Otc.PubSub.PunchySubscriber.Abstractions
{
    public class PunchyMessage
    {
        public byte[] MessageBytes { get; set; }
        public string Topic { get; set;  }
        public DateTimeOffset Timestamp { get; set; }
        public IEnumerable<Attempt> Attempts { get; set; }
        public IDictionary<string, object> MessageAddress { get; set; }
    }
}