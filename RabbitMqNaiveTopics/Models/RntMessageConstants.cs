using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Models
{
    public static class RntMessageConstants
    {
        public const string RetryCountHeader = "rnt-retry-count";
        public const string RetryRoutingKeyHeader = "rnt-retry-routing-key";
        public const string RetryTopicHeader = "rnt-retry-topic";
        public const string DelayHeader = "rnt-delay";
        public const string RetrySubscription = "snt-retry-subscription-name";

        public const string RetryRoutingKey = "dlq.retry";
        public const string DeadLetterRoutingKey = "dlq.non_retry";
    }
}
