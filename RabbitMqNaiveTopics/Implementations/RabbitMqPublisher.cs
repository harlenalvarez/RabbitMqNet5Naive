﻿using Microsoft.Extensions.Logging;
using RabbitMqNaiveTopics.Extensions;
using RabbitMqNaiveTopics.Interfaces;
using RabbitMqNaiveTopics.Models;
using System;
using System.Collections.Generic;

namespace RabbitMqNaiveTopics.Implementations
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IChannelFactory _channelFactory;
        private readonly IMessageParser _parser;

        public RabbitMqPublisher(IChannelFactory channelFactory, IMessageParser parser)
        {
            this._channelFactory = channelFactory;
            this._parser = parser;
        }

        public void SendMessage<T>(string topic, T payload, string filterKey = "#", TimeSpan? expiration = null, string userId = null)
        {
            var message = _parser.SerializeMessage(payload);
            var channel = _channelFactory.GetChannel(topic);
            
            var options = channel.CreateBasicProperties();
            options.Persistent = true;
            options.Expiration = expiration.HasValue ?
                expiration.Value.TotalMilliseconds.ToString() :
                TimeSpan.FromHours(24).TotalMilliseconds.ToString();
            options.Headers = new Dictionary<string, object>();
            options.Headers.Add(RntMessageConstants.RetryCountHeader, 0);
            options.Type = typeof(T).AssemblyQualifiedName;
            if(!string.IsNullOrEmpty(userId))
            {
                options.SetRequestUserId(userId);
            }
            channel.BasicPublish(exchange: topic, routingKey: filterKey, false, options, message);
        }

    }
}
