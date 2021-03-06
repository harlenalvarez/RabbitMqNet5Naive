using RabbitMQ.Client;
using RabbitMqNaiveTopics.Interfaces;
using RabbitMqNaiveTopics.Models;
using System;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.BaseImplementation
{
    public abstract class BaseMessageSubscriber<T>: IMessageSubscriber
    {
        private readonly IMessageParser _messageParser;

        public BaseMessageSubscriber(IMessageParser messageParser)
        {
            this._messageParser = messageParser;
        }

        public virtual ushort PrefetchCount { get; } = 5;
        public virtual string ForwardToRoutingKey { get; } = "#";
        public virtual ushort MaxRetries { get; } = 3;
        public abstract string TopicName { get; }
        public bool IsDeadLetter { get; } = false;
        public virtual string SubscriptionName { get; } = null;
        public abstract ValueTask<MessageSubscriberResponse> HandleAsync(T message, IBasicProperties properties);

        public ValueTask<MessageSubscriberResponse> HandleAsync(ReadOnlyMemory<byte> message, IBasicProperties properties)
        {
            T payload = _messageParser.ParseMessage<T>(message.Span);
            return HandleAsync(payload, properties);
        }
    }
}
