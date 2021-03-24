using RabbitMQ.Client;
using RabbitMqNaiveTopics.Interfaces;
using RabbitMqNaiveTopics.Models;
using System;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.BaseImplementation
{
    public abstract class BaseMessageDeadLetterSubscriber : IMessageSubscriber
    {
        public virtual ushort PrefetchCount { get; } = 1;
        public virtual ushort MaxRetries { get; } = 1;
        public string TopicName { get; } = string.Empty;
        public string ForwardToRoutingKey { get; } = "dlq.non_retry";
        public bool IsDeadLetter { get; } = true;
        public abstract ValueTask<MessageSubscriberResponse> HandleAsync(ReadOnlyMemory<byte> message, IBasicProperties properties);
    }
}
