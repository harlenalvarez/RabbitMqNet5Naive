using System;
using RabbitMQ.Client;

namespace RabbitMqNaiveTopics.Interfaces
{
    public interface IChannelFactory : IDisposable
    {
        IModel GetChannel(string topicName, bool isDeadLetter = false);
    }
}
