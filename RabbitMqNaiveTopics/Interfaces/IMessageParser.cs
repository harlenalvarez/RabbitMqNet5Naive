using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Interfaces
{
    public interface IMessageParser
    {
        T ParseMessage<T>(ReadOnlySpan<byte> payload);
        byte[] SerializeMessage<T>(T payload);
    }
}
