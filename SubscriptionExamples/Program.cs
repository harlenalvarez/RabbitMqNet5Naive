using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMqNaiveTopics;
using RabbitMqNaiveTopics.BaseImplementation;
using RabbitMqNaiveTopics.Interfaces;
using RabbitMqNaiveTopics.Models;
using System;
using System.Threading.Tasks;

namespace SubscriptionExamples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddRabbitMQMessagingService(opt =>
                    {
                        opt.ConnectionString = hostContext.Configuration.GetConnectionString("RabbitMQ");
                    })
                    .RegisterSubsciber<TestSub1>()
                    //.RegisterSubsciber<TestSub2>()
                    .RegisterSubsciber<DLQSub>()
                    .UseSubscribers();
                });
    }

    public class TestSub1 : BaseMessageSubscriber<string>
    {
        private readonly ILogger<TestSub1> logger;

        public TestSub1(IMessageParser parser, ILogger<TestSub1> logger) : base(parser)
        {
            this.logger = logger;
        }
        public override string TopicName => "test-topic";

        public override ushort PrefetchCount => 1;

        public override async ValueTask<MessageSubscriberResponse> HandleAsync(string message, IBasicProperties properties)
        {
            await Task.Delay(1000);
            logger.LogInformation($"Receive Message in Sub1 {message}");
            return new MessageSubscriberResponse
            {
                Success = false,
                ShouldRetry = true
            };
        }
    }

    public class TestSub2 : BaseMessageSubscriber<string>
    {
        private readonly ILogger<TestSub2> logger;

        public TestSub2(IMessageParser messageParser, ILogger<TestSub2> logger) : base(messageParser)
        {
            this.logger = logger;
        }
        public override string TopicName => "test-topic";
        public override ushort PrefetchCount => 1;

        public override async ValueTask<MessageSubscriberResponse> HandleAsync(string message, IBasicProperties properties)
        {
            var random = new Random();
            await Task.Delay(1000);
            logger.LogInformation($"Receive Message in Sub2 {message}");
            return new MessageSubscriberResponse
            {
                Success = true
            };
        }
    }

    public class DLQSub : BaseMessageDeadLetterSubscriber
    {
        private readonly ILogger<DLQSub> logger;

        public DLQSub(ILogger<DLQSub> logger)
        {
            this.logger = logger;
        }
        public override async ValueTask<MessageSubscriberResponse> HandleAsync(ReadOnlyMemory<byte> messsage, IBasicProperties properties)
        {
            logger.LogInformation($"Receive deadletter message");
            return new MessageSubscriberResponse
            {
                Success = true
            };
        }
    }
}
