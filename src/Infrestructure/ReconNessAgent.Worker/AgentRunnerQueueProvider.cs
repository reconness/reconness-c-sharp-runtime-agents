using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ReconNessAgent.Worker
{
    internal class AgentRunnerQueueProvider
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<Worker> _logger;

        private IModel channel;

        private string queueName;

        public AgentRunnerQueueProvider(IConfiguration configuration, ILogger<Worker> _logger)
        {
            this.configuration = configuration;
            this._logger = _logger;
        }

        public void Start()
        {
            try
            {
                var rabbitmqConnectionString = this.configuration.GetConnectionString("DefaultRabbitmqConnection");

                var rabbitMQUserName = Environment.GetEnvironmentVariable("RabbitMQUser") ??
                                     Environment.GetEnvironmentVariable("RabbitMQUser", EnvironmentVariableTarget.User);
                var rabbitMQPassword = Environment.GetEnvironmentVariable("RabbitMQPassword") ??
                                 Environment.GetEnvironmentVariable("RabbitMQPassword", EnvironmentVariableTarget.User);

                rabbitmqConnectionString = rabbitmqConnectionString.Replace("{{username}}", rabbitMQUserName)
                                                                   .Replace("{{password}}", rabbitMQPassword);

                var reconnessAgentOrderFromEnv = Environment.GetEnvironmentVariable("ReconnessAgentOrder") ??
                             Environment.GetEnvironmentVariable("ReconnessAgentOrder", EnvironmentVariableTarget.User);

                if(!int.TryParse(reconnessAgentOrderFromEnv, out int reconnessAgentOrder))
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
                _logger.LogError(ex.Message);
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
                            _logger.LogInformation(Encoding.UTF8.GetString(body));
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
