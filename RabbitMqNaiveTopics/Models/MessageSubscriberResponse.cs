using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMqNaiveTopics.Models
{
    public record MessageSubscriberResponse
    {
        public bool Success { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// If message was not successfull should you retry
        /// </summary>
        public bool ShouldRetry { get; set; } = false;
    }
}
