using Microsoft.Extensions.DependencyInjection;
using RabbitMqNaiveTopics.Interfaces;

namespace RabbitMqNaiveTopics.Implementations
{
    public class MessagingServiceRegistration
    {
        private readonly IServiceCollection _services;

        public MessagingServiceRegistration(IServiceCollection services)
        {
            this._services = services;
        }

        /// <summary>
        /// Registers a topic subscriber.  Most call UseSubscribers at the end of registration in order to receive messages
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="PrefetchCount">Determines how many messages each worker can recieve.  The subscription size (queue size) is unlimited</param>
        /// <returns></returns>
        public MessagingServiceRegistration RegisterSubsciber<T>() where T : IMessageSubscriber
        {
            _services.AddSingleton(typeof(IMessageSubscriber), typeof(T));
            return this;
        }

        /// <summary>
        /// Adds background service to handle all the registration
        /// </summary>
        public void UseSubscribers()
        {
            _services.AddHostedService<SubscribersBackgroundService>();
            _services.AddHostedService<DeadLetterBackgroundService>();
        }
    }
}
