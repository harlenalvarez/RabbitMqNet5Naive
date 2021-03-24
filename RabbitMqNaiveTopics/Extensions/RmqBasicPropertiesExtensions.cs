using RabbitMQ.Client;
using RabbitMqNaiveTopics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Extensions
{
    public static class RmqBasicPropertiesExtensions
    {
        public static int GetRetryCount(this IBasicProperties properties)
        {
            if (!properties.IsHeadersPresent())
                return 0;
            if (!properties.Headers.TryGetValue(RntMessageConstants.RetryCountHeader, out var headerValue))
                return 0;
            return (int)headerValue;
        }

        public static void SetRetryCount(this IBasicProperties properties, int value)
        {
            properties.EnsureHeadersExist();
            if (properties.Headers.ContainsKey(RntMessageConstants.RetryCountHeader))
            {
                properties.Headers[RntMessageConstants.RetryCountHeader] = value;
            }
            else
            {
                properties.Headers.Add(RntMessageConstants.RetryCountHeader, value);
            }
        }

        public static string GetRoutingKey(this IBasicProperties properties)
        {
            if (!properties.IsHeadersPresent()) return string.Empty;
            if (!properties.Headers.TryGetValue(RntMessageConstants.RetryRoutingKeyHeader, out var routingKeyValue)) return string.Empty;

            return Encoding.UTF8.GetString((byte[])routingKeyValue);
        }

        public static void SetRoutingKey(this IBasicProperties properties, string routingKey)
        {
            properties.EnsureHeadersExist();
            if (!properties.Headers.ContainsKey(RntMessageConstants.RetryRoutingKeyHeader))
                properties.Headers.Add(RntMessageConstants.RetryRoutingKeyHeader, routingKey);
            else
                properties.Headers[RntMessageConstants.RetryRoutingKeyHeader] = routingKey;
        }

        public static string GetRetryTopic(this IBasicProperties properties)
        {
            if (!properties.IsHeadersPresent()) return string.Empty;
            if (!properties.Headers.TryGetValue(RntMessageConstants.RetryTopicHeader, out var topicName)) return string.Empty;

            return Encoding.UTF8.GetString((byte[])topicName);
        }

        public static void SetRetryTopic(this IBasicProperties properties, string topicName)
        {
            properties.EnsureHeadersExist();
            if (!properties.Headers.ContainsKey(RntMessageConstants.RetryTopicHeader))
                properties.Headers.Add(RntMessageConstants.RetryTopicHeader, topicName);
            else
                properties.Headers[RntMessageConstants.RetryTopicHeader] = topicName;
        }

        public static string GetRetrySubscription(this IBasicProperties properties)
        {
            if (!properties.IsHeadersPresent()) return string.Empty;
            if (!properties.Headers.TryGetValue(RntMessageConstants.RetrySubscription, out var subscriptionName)) return string.Empty;

            return Encoding.UTF8.GetString((byte[])subscriptionName);
        }

        public static void SetRetrySubscription(this IBasicProperties properties, string subscriptionName)
        {
            properties.EnsureHeadersExist();
            if (!properties.Headers.ContainsKey(RntMessageConstants.RetrySubscription))
                properties.Headers.Add(RntMessageConstants.RetrySubscription, subscriptionName);
            else
                properties.Headers[RntMessageConstants.RetrySubscription] = subscriptionName;
        }

        public static void EnsureHeadersExist(this IBasicProperties properties)
        {
            if(!properties.IsHeadersPresent())
               properties.Headers = new Dictionary<string, object>();
        }
    }
}
