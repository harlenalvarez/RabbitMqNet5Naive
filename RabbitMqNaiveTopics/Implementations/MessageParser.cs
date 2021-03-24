using RabbitMqNaiveTopics.Interfaces;
using System;
using System.Text.Json;

namespace RabbitMqNaiveTopics.Implementations
{
    public class MessageParser : IMessageParser
    {
        private JsonSerializerOptions _options;

        public MessageParser()
        {
            _options = new JsonSerializerOptions { IgnoreNullValues = true };
        }

        public byte[] SerializeMessage<T>(T payload)
        {
            return JsonSerializer.SerializeToUtf8Bytes(payload, _options);
        }

        public T ParseMessage<T>(ReadOnlySpan<byte> payload)
        {
            return JsonSerializer.Deserialize<T>(payload, _options);
        }
    }
}
