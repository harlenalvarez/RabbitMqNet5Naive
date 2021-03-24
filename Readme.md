## Rabbit MQ Net5 Naive Library

Basic library to consume Rabbit MQ in.Net 5 using the Topic / Sub model.  
Manages connections, messages, subscriptions and topics are persisted by default, supports multiple instances and prefetching, 
has a basic retry queue that re-publishes messages to the failed subscriber,
and a dead letter queue subscription.

# Getting Started
1. Install the package
```csharp
dotnet add package RabbitMqNet5Naive
```

2. Register the library in your service collection.
```csharp
services.AddRabbitMQMessagingService(opt =>
{
    opt.ConnectionString = config.GetConnectionString("RabbitMQ");
});
```

3. Publish a message
```csharp
var publisher = provider.GetService<IMessagePublisher>();
publisher.SendMessage<string>("test-topic", "Test Topic payload");
```

4. To handle messages create a subscription handler.
```csharp
    public class TestSub1 : BaseMessageSubscriber<string>
    {
        public TestSub1(IMessageParser parser): base(parser)
        {}

        public override string TopicName => "test-topic";

        public override async ValueTask<MessageSubscriberResponse> HandleAsync(string message)
        {
            //Do work
            Console.WriteLine($"Receive Message in Sub1 {message}");
            return new MessageSubscriberResponse
            {
                Success = true
            };
        }
    }
```

5. Register the subscriber in a Worker Service or Api Project (You can also do it in a console app but will need to set up a Host Builder)
```csharp
services.AddRabbitMQMessagingService(opt =>
{
    opt.ConnectionString = hostContext.Configuration.GetConnectionString("RabbitMQ");
})
.RegisterSubsciber<TestSub1>()
.UseSubscribers();
```

6. To handle dead letters create a dead letter subscriber (you can only have one)
```csharp
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
```
7. Register the DLQ Handler
```csharp
services.AddRabbitMQMessagingService(opt =>
{
    opt.ConnectionString = hostContext.Configuration.GetConnectionString("RabbitMQ");
})
.RegisterSubsciber<TestSub1>()
.RegisterSubsciber<DLQSub>() // Register here
.UseSubscribers();
```

### Options
##### Registration Options

| Property | Required | Default Value    | Purpose
| --- | --- | --- | --- |
|ConnectionString| Yes      | no default value | The rabbitmq connection string
|RetrySubscription| Yes     | RMS_RetrySubscription | Name of the retry subscription that is created in the background
|DeadLetterSubscription| Yes | RMS_DeadLetterSubscription| Name of the deadletter subscription
|DeadLetterTopic| Yes | RMS_DeadLetterTopic | Name of the dead letter topic that will recieve retry and dlq messages

##### Subscriber Options


| Property       | Required | Default Value    | Purpose
| --- | --- | --- | --- |
| TopicName      | yes      | no default value | Name of topic to get messages from
| ForwardToRoutingKey | no | # | This is the routing key for the subscription, only messages with this key will be picked up by the subscription.
| PrefetchCount | yes | 5 | Number of messages to prefetch, messages will still be processed async and it will distribute between multiple instances
| MaxRetries | yes | 3 | Max number of retries it will perform before sending the message to the DLQ

