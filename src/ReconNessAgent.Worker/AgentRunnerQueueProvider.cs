using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;

namespace ReconNessAgent.Worker
{
    internal class AgentRunnerQueueProvider
    {
        private readonly ILogger<Worker> _logger;

        private readonly IModel channel;

        public AgentRunnerQueueProvider(IConfiguration configuration, ILogger<Worker> _logger)
        {
            var rabbitmqConnectionString = configuration.GetConnectionString("DefaultRabbitmqConnection");

            var rabbitMQUserName = Environment.GetEnvironmentVariable("RabbitMQUser") ??
                                 Environment.GetEnvironmentVariable("RabbitMQUser", EnvironmentVariableTarget.User);
            var rabbitMQPassword = Environment.GetEnvironmentVariable("RabbitMQPassword") ??
                             Environment.GetEnvironmentVariable("RabbitMQPassword", EnvironmentVariableTarget.User);

            rabbitmqConnectionString = rabbitmqConnectionString.Replace("{{username}}", rabbitMQUserName)
                                                               .Replace("{{password}}", rabbitMQPassword);

            var factory = new ConnectionFactory() { Uri = new Uri(rabbitmqConnectionString) };

            var conn = factory.CreateConnection();

            this.channel = conn.CreateModel();

            this.channel.ExchangeDeclare("reconness", ExchangeType.Direct);
            this.channel.QueueDeclare("reconness-queue");

            this.channel.QueueBind("reconness-queue", "reconness", "reconness");

            this._logger = _logger;
        }

        public void Consumer()
        {            
            var consumer = new EventingBasicConsumer(this.channel);
            consumer.Received += (ch, ea) =>
            {
                var body = ea.Body.ToArray();
                if (body != null)
                    _logger.LogInformation(Encoding.UTF8.GetString(body));

                channel.BasicAck(ea.DeliveryTag, false);
            };

            channel.BasicConsume("reconness-queue", false, consumer);
        }
    }
}
