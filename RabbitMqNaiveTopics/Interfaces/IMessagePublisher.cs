using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Interfaces
{
    public interface IMessagePublisher
    {
        void SendMessage<T>(string topic, T payload, string filterKey = "#", TimeSpan? expiration = null, string userId = null);
    }
}
