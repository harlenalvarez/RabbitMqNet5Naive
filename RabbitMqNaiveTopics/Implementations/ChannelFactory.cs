using RabbitMQ.Client;
using RabbitMqNaiveTopics.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RabbitMqNaiveTopics.Implementations
{
    public class ChannelFactory : IChannelFactory
    {
        private readonly IConnection _connection;
        private static ConcurrentDictionary<string, IModel> _channels = new ConcurrentDictionary<string, IModel>();
        public ChannelFactory(IConnection connection)
        {
            this._connection = connection;
        }
        public void Dispose()
        {
            foreach (var channel in _channels)
            {
                if (!channel.Value.IsClosed)
                {
                    channel.Value.Close();
                }
            }
        }

        public IModel GetChannel(string topicName, bool isDeadLetter = false)
        {
            IModel model;
            model = _channels.GetOrAdd(topicName, GetTopicChannel(topicName, isDeadLetter));
            if (model.IsClosed)
            {
                // Remove it from channels
                _channels.Remove(topicName, out model);

                //Re - add it
                model = _channels.GetOrAdd(topicName, GetTopicChannel(topicName, isDeadLetter));
            }
            return model;
        }

        private IModel GetTopicChannel(string topic, bool isDeadLetter)
        {
            var channel = _connection.CreateModel();
            if (isDeadLetter)
            {
                channel.ExchangeDeclare(topic, ExchangeType.Direct, true);
            }
            else
            {
                channel.ExchangeDeclare(topic, ExchangeType.Topic, true);
            }
            return channel;
        }
    }
}
