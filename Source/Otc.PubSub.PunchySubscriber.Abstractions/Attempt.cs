using System;
using System.Collections.Generic;
using System.Text;

namespace Otc.PubSub.PunchySubscriber.Abstractions
{
    public class Attempt
    {
        public DateTimeOffset Timestamp { get; set; }
        public IDictionary<string, object> MessageAddress { get; set; }
        public string Topic { get; set; }
        public BadMessageContents BadMessageContents { get; set; }
    }
}
