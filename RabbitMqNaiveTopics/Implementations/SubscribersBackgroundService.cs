using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqNaiveTopics.Extensions;
using RabbitMqNaiveTopics.Interfaces;
using RabbitMqNaiveTopics.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Implementations
{
    public class SubscribersBackgroundService : BackgroundService
    {
        private readonly IDictionary<string, IMessageSubscriber> subscriptionsCollection;
        private readonly IServiceProvider _serviceProvider;
        private readonly IChannelFactory _channelFactory;
        private readonly IOptions<MessagingServiceOptions> messagingServiceOptions;
        private readonly ILogger<SubscribersBackgroundService> _logger;
        private readonly ConcurrentDictionary<string, IModel> subscriptionChannels = new ConcurrentDictionary<string, IModel>();

        public SubscribersBackgroundService(IServiceProvider serviceProvider, IChannelFactory channelFactory, IOptions<MessagingServiceOptions> messagingServiceOptions, ILogger<SubscribersBackgroundService> logger)
        {
            this._serviceProvider = serviceProvider;
            this._channelFactory = channelFactory;
            this.messagingServiceOptions = messagingServiceOptions;
            this._logger = logger;
            subscriptionsCollection = _serviceProvider
                .GetServices<IMessageSubscriber>()
                .Where(s => !s.IsDeadLetter)
                .ToDictionary(s => s.GetType().Name, s => s);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topics = subscriptionsCollection.Select(s => s.Value.TopicName).ToHashSet<string>();
            var args = new Dictionary<string, object>();
            args.Add("x-dead-letter-exchange", messagingServiceOptions.Value.DeadLetterTopic);
            args.Add("x-dead-letter-routing-key", RntMessageConstants.DeadLetterRoutingKey);
            foreach (var topic in topics)
            {
                IModel channel = _channelFactory.GetChannel(topic);
                List<KeyValuePair<string, IMessageSubscriber>> topicSubscriptions = subscriptionsCollection
                    .Where(s => s.Value.TopicName == topic)
                    .ToList();
                foreach ((string nameOfSubscription, var subscription) in topicSubscriptions)
                {
                    var routingKey = subscriptionRoutingKey(nameOfSubscription, subscription.ForwardToRoutingKey);
                    channel.QueueDeclare(nameOfSubscription, true, false, false, args);
                    channel.BasicQos(0, subscription.PrefetchCount, false);
                    channel.QueueBind(nameOfSubscription, topic, routingKey);
                    subscriptionChannels.AddOrUpdate(nameOfSubscription, channel, (key, oldChannel) => channel);
                    var consumer = new AsyncEventingBasicConsumer(channel);
                    consumer.Received += async (model, args) =>
                    {
                        await ProcessMessage(args, nameOfSubscription).ConfigureAwait(false);
                    };
                    channel.BasicConsume(nameOfSubscription, false, consumer);
                }
            }
            return Task.CompletedTask;
        }

        private async Task ProcessMessage(BasicDeliverEventArgs args, string subscriptionName)
        {
            if(subscriptionChannels.TryGetValue(subscriptionName, out var channel))
            {
                if (!subscriptionsCollection.TryGetValue(subscriptionName, out var subscription)) throw new Exception("No subscription found to handle this message");
                // Before we do anything lets check if this is a retried message and is meant for another subscription
                var retryCount = args.BasicProperties.GetRetryCount();
                if(retryCount > 0 && subscriptionName != args.BasicProperties.GetRetrySubscription())
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }
                ReadOnlyMemory<byte> content = args.Body;
                MessageSubscriberResponse response = await subscription.HandleAsync(content, args.BasicProperties);

                if (response.Success)
                {
                    channel.BasicAck(args.DeliveryTag, false);
                    return;
                }

                if (response.ShouldRetry)
                {
                    if (retryCount >= subscription.MaxRetries)
                    {
                        channel.BasicReject(args.DeliveryTag, false);
                        return;
                    }

                    Memory<byte> payloadClone = new Memory<byte>(args.Body.ToArray());
                    var routingKey = RntMessageConstants.RetryRoutingKey;
                    var argsProperties = channel.CreateBasicProperties();
                    argsProperties.Headers = new Dictionary<string, object>();
                    argsProperties.Persistent = args.BasicProperties.Persistent;
                    argsProperties.Expiration = args.BasicProperties.Expiration;
                    argsProperties.CorrelationId = args.BasicProperties.CorrelationId;
                    argsProperties.UserId = args.BasicProperties.UserId;
                    argsProperties.AppId = args.BasicProperties.AppId;
                    argsProperties.ContentEncoding = args.BasicProperties.ContentEncoding;
                    argsProperties.ContentType = args.BasicProperties.ContentType;
                    foreach ((var k, var v) in args.BasicProperties.Headers)
                    {
                        argsProperties.Headers.Add(k, v);
                    }
                    argsProperties.SetRoutingKey(args.RoutingKey);
                    argsProperties.SetRetryTopic(subscription.TopicName);
                    argsProperties.SetRetrySubscription(subscriptionName);

                    channel.BasicAck(args.DeliveryTag, false);
                    channel.BasicPublish(exchange: messagingServiceOptions.Value.DeadLetterTopic, routingKey: routingKey, false, argsProperties, payloadClone);
                    return;
                }

                channel.BasicReject(args.DeliveryTag, false);
            }
        }


        private string subscriptionRoutingKey(string subscriptionName, string routingKey = null)
        {
            return string.IsNullOrEmpty(routingKey) ? $"{subscriptionName}.key" : routingKey;
        }
    }
}
