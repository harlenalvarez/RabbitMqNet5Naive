using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMqNaiveTopics.Implementations;
using RabbitMqNaiveTopics.Interfaces;
using System;

namespace RabbitMqNaiveTopics
{
    public static class MessagingServiceExtensions
    {
        /// <summary>
        /// Add Rabbit MQ messaging service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static MessagingServiceRegistration AddRabbitMQMessagingService(this IServiceCollection services, Action<MessagingServiceOptions> config)
        {
            services
                .AddOptions<MessagingServiceOptions>()
                .Configure(config);
            var msOptions = new MessagingServiceOptions();
            config(msOptions);
            var connectionFactory = new ConnectionFactory { DispatchConsumersAsync = true };
            connectionFactory.Uri = new Uri(msOptions.ConnectionString);
            var conn = connectionFactory.CreateConnection();
            services.AddSingleton<IConnection>(conn);
            services.AddSingleton<IMessageParser, MessageParser>();
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            services.AddSingleton<IChannelFactory, ChannelFactory>();
            var listeners = new MessagingServiceRegistration(services);
            return listeners;
        }
    }

    public class MessagingServiceOptions
    {
        /// <summary>
        /// Rabbit amqp connection string 
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// Name of retry subscription, can only have one
        /// </summary>
        public string RetrySubscription { get; set; } = "RMS_RetrySubscription";
        /// <summary>
        /// Name of dead letter subscription, can only have one
        /// </summary>
        public string DeadLetterSubscription { get; set; } = "RMS_DeadLetterSubscription";
        /// <summary>
        /// Dead letter topic where all dead letters go to
        /// </summary>
        public string DeadLetterTopic { get; set; } = "RMS_DeadLetterTopic";
    }
}
