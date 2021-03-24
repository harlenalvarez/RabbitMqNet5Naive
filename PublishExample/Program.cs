using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMqNaiveTopics;
using RabbitMqNaiveTopics.Interfaces;
using System;

namespace PublishExample
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Program>()
                .Build();
            services.AddLogging(config => config.AddConsole());
            services.AddRabbitMQMessagingService(opt =>
            {
                opt.ConnectionString = config.GetConnectionString("RabbitMQ");
            });

            var provider = services.BuildServiceProvider();
            var publisher = provider.GetService<IMessagePublisher>();
            var userId = Guid.NewGuid();
            for (int i = 1; i < 3; i++)
                publisher.SendMessage<string>("test-topic", $"Test Topic payload {i}", userId: userId.ToString());
            Console.WriteLine("Published message");
        }
    }
}
