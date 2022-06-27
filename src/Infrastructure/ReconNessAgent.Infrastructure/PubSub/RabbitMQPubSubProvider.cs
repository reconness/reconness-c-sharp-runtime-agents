using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReconNessAgent.Application;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using Serilog;
using System.Text;

namespace ReconNessAgent.Infrastructure.PubSub;

/// <summary>
/// This class implement the interface <see cref="IPubSubProvider"/> to consume message from the pub/sub provider that we are using.
/// This particular implementation is using RabbitMQ as pub/sub provider.
/// </summary>
public class RabbitMQPubSubProvider : IPubSubProvider
{
    private static readonly ILogger _logger = Log.ForContext<RabbitMQPubSubProvider>();

    private readonly PubSubOptions options;
    private readonly IAgentService agentService;
    private readonly IServiceProvider serviceProvider;

    private IModel? channel;

    private string? queueName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RabbitMQPubSubProvider" /> class.
    /// </summary>
    /// <param name="agentService"><see cref="IAgentService"/></param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/></param>
    /// <param name="options">The configuraion options</param>
    public RabbitMQPubSubProvider(IAgentService agentService, IServiceProvider serviceProvider, PubSubOptions options)
    {
        this.agentService = agentService;
        this.serviceProvider = serviceProvider;
        this.options = options;
    }

    /// <inheritdoc/>
    public Task ConsumerAsync(CancellationToken stoppingToken)
    {
        if (this.channel == null)
        {
            this.InitializeChannel();

            this.SubscribeConsumerEvent(stoppingToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribe to the RabbitMQ consumer event 
    /// </summary>
    private void SubscribeConsumerEvent(CancellationToken cancellationToken)
    {
        if (this.channel != null)
        {
            var consumer = new AsyncEventingBasicConsumer(this.channel);
            consumer.Received += async (ch, ea) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var body = ea.Body.ToArray();
                if (body != null)
                {
                    using var scope = this.serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();

                    var agentInfoJson = Encoding.UTF8.GetString(body);
                    await this.agentService.RunAsync(unitOfWork!, agentInfoJson, cancellationToken);

                    _logger.Information(agentInfoJson);
                }

                channel.BasicAck(ea.DeliveryTag, false);
            };

            channel.BasicConsume(this.queueName, false, consumer);
        }
    }

    /// <summary>
    /// Try to initialize the channel .
    /// </summary>
    private void InitializeChannel()
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
}