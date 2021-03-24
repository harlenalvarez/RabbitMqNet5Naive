using RabbitMQ.Client;
using RabbitMqNaiveTopics.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Interfaces
{
    public interface IMessageSubscriber
    {
        /// <summary>
        /// Name of the topic
        /// </summary>
        string TopicName { get; }
        /// <summary>
        /// A key to filter messages to this subscriber, default to # to fanout
        /// </summary>
        string ForwardToRoutingKey { get; }
        /// <summary>
        /// Count of how many messages can an instance of a subscription fetch from the db. Default is 5.
        /// </summary>
        ushort PrefetchCount { get; }

        /// <summary>
        /// The amount of times it can requeue a message before giving up
        /// </summary>
        ushort MaxRetries { get; }

        /// <summary>
        /// Subscriber handles dead letters
        /// </summary>
        bool IsDeadLetter { get; }


        ValueTask<MessageSubscriberResponse> HandleAsync(ReadOnlyMemory<byte> messsage, IBasicProperties properties);
    }
}
