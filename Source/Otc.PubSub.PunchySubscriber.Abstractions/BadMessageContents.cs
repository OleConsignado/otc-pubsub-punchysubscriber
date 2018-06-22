using System;
using System.Collections.Generic;
using System.Text;

namespace Otc.PubSub.PunchySubscriber.Abstractions
{
    public class BadMessageContents
    {
        public IDictionary<string, object> SourceMessageAddress { get; set; }
        public Exception Exception { get; set; }
    }
}
