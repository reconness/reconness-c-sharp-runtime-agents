using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReconNessAgent.Application;
using ReconNessAgent.Application.Models;
using Serilog;
using System.Text;

namespace ReconNessAgent.PubSub
{
    public class RabbitMQPubSubProvider : IPubSubProvider
    {
        private static readonly ILogger _logger = Log.ForContext<RabbitMQPubSubProvider>();

        private readonly PubSubOptions options;

        private IModel channel;

        private string queueName;

        public RabbitMQPubSubProvider(IOptions<PubSubOptions> options)
        {
            this.options = options.Value;
        }

        public void Start()
        {
            try
            {
                var rabbitmqConnectionString = this.options.ConnectionString;

                var rabbitMQUserName = Environment.GetEnvironmentVariable("RabbitMQUser") ??
                                     Environment.GetEnvironmentVariable("RabbitMQUser", EnvironmentVariableTarget.User);
                var rabbitMQPassword = Environment.GetEnvironmentVariable("RabbitMQPassword") ??
                                 Environment.GetEnvironmentVariable("RabbitMQPassword", EnvironmentVariableTarget.User);

                rabbitmqConnectionString = rabbitmqConnectionString.Replace("{{username}}", rabbitMQUserName)
                                                                   .Replace("{{password}}", rabbitMQPassword);

                var reconnessAgentOrderFromEnv = Environment.GetEnvironmentVariable("ReconnessAgentOrder") ??
                             Environment.GetEnvironmentVariable("ReconnessAgentOrder", EnvironmentVariableTarget.User);

                if (!int.TryParse(reconnessAgentOrderFromEnv, out int reconnessAgentOrder))
                {
                    reconnessAgentOrder = 1;
                }

                var factory = new ConnectionFactory() { Uri = new Uri(rabbitmqConnectionString), DispatchConsumersAsync = true };

                var conn = factory.CreateConnection();

                this.channel = conn.CreateModel();

                this.channel.ExchangeDeclare("reconness", ExchangeType.Direct);
                var queue = this.channel.QueueDeclare("");
                this.queueName = queue.QueueName;

                this.channel.QueueBind(this.queueName, "reconness", $"reconness-{reconnessAgentOrder}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                this.channel = null;
            }
        }

        public void Consumer()
        {
            if (this.channel == null)
            {
                this.Start();
                if (this.channel != null)
                {
                    var consumer = new AsyncEventingBasicConsumer(this.channel);
                    consumer.Received += async (ch, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        if (body != null)
                        {
                            _logger.Information(Encoding.UTF8.GetString(body));
                            await Task.Delay(10000);
                        }

                        channel.BasicAck(ea.DeliveryTag, false);
                    };

                    channel.BasicConsume(this.queueName, false, consumer);
                }
            }
        }
    }
}