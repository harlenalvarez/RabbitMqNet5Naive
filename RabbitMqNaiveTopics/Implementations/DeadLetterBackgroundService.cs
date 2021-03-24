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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Implementations
{
    public class DeadLetterBackgroundService : BackgroundService
    {
        private readonly IChannelFactory channelFactory;
        private readonly IOptions<MessagingServiceOptions> messagingServiceOptions;
        private readonly ILogger<DeadLetterBackgroundService> logger;
        private readonly IMessageSubscriber subscription;
        public DeadLetterBackgroundService(IServiceProvider serviceProvider, IChannelFactory channelFactory, IOptions<MessagingServiceOptions> messagingServiceOptions,  ILogger<DeadLetterBackgroundService> logger)
        {
            this.channelFactory = channelFactory;
            this.messagingServiceOptions = messagingServiceOptions;
            this.logger = logger;

            subscription = serviceProvider
                .GetServices<IMessageSubscriber>()
                .Where(s => s.IsDeadLetter).FirstOrDefault();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IModel channel = channelFactory.GetChannel(messagingServiceOptions.Value.DeadLetterTopic, true);
            RegisterRetrySubscription(channel);
            RegisterDeadLetterSubscription(channel);
            return Task.CompletedTask;
        }

        private void RegisterRetrySubscription(IModel channel)
        {
            var args = new Dictionary<string, object>();
            args.Add("x-dead-letter-exchange", messagingServiceOptions.Value.DeadLetterSubscription);
            args.Add("x-dead-letter-routing-key", RntMessageConstants.DeadLetterRoutingKey);
            channel.QueueDeclare(messagingServiceOptions.Value.RetrySubscription, true, false, false, args);
            channel.BasicQos(0, 1, false);
            channel.QueueBind(messagingServiceOptions.Value.RetrySubscription, messagingServiceOptions.Value.DeadLetterTopic, RntMessageConstants.RetryRoutingKey);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (model, args) =>
            {
                var currentRetryCount = args.BasicProperties.GetRetryCount();
                var routingKey = args.BasicProperties.GetRoutingKey();
                var retryTopic = args.BasicProperties.GetRetryTopic();

                if(string.IsNullOrEmpty(routingKey) || string.IsNullOrEmpty(retryTopic))
                {
                    // We don't have a routing key to send back to so we'll dlq
                    channel.BasicNack(args.DeliveryTag, false, false);
                    return;
                }
                Memory<byte> payloadClone = new Memory<byte>(args.Body.ToArray());
               
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
                argsProperties.SetRetryCount(currentRetryCount + 1);
                channel.BasicAck(args.DeliveryTag, false);
                _ = DelayAndPublish(
                    messagingServiceOptions.Value.RetrySubscription,
                    retryTopic,
                    routingKey,
                    argsProperties, payloadClone, channel);
            };
            channel.BasicConsume(messagingServiceOptions.Value.RetrySubscription, false, consumer);
        }

        private void RegisterDeadLetterSubscription(IModel channel)
        {
            if (subscription == null) return;
            channel.QueueDeclare(messagingServiceOptions.Value.DeadLetterSubscription, true, false, false);
            channel.BasicQos(0, 1, false);
            channel.QueueBind(messagingServiceOptions.Value.DeadLetterSubscription, messagingServiceOptions.Value.DeadLetterTopic, RntMessageConstants.DeadLetterRoutingKey);
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (model, args) =>
            {
                var result = await subscription.HandleAsync(args.Body, args.BasicProperties).ConfigureAwait(false);
                if (result.Success)
                {
                    channel.BasicAck(args.DeliveryTag, false);
                }
                else
                {
                    // Won't requeue if it has been requeued already
                    channel.BasicNack(args.DeliveryTag, false, !args.Redelivered);
                }
            };
            channel.BasicConsume(messagingServiceOptions.Value.DeadLetterSubscription, false, consumer);
        }

        private async Task DelayAndPublish(string subscriptionName, string topicName, string routingKey, IBasicProperties properties, ReadOnlyMemory<byte> payload, IModel channel)
        {
            var randomDelay = new Random();
            double delay;
            if (!properties.Headers.TryGetValue(RntMessageConstants.DelayHeader, out var delayObject))
            {
                delay = 500;
            }
            else
            {
                delay = (double)delayObject > 0 ? (double)delayObject : 500;
            }

            var delayWithJitter = TimeSpan.FromMilliseconds((delay * 2) + randomDelay.Next(300, 1000)); // We don't want to wait too long, if service crashes this messages would be lost. We don't want that
            await Task.Delay(delayWithJitter).ConfigureAwait(false);
            channel.BasicPublish(exchange: topicName, routingKey: routingKey, false, properties, payload);
        }
    }
}
