﻿using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReconNessAgent.Application;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using Serilog;
using System.Text;

namespace ReconNessAgent.Infrastructure.PubSub;

/// <summary>
/// 
/// </summary>
public class RabbitMQPubSubProvider : IPubSubProvider
{
    private static readonly ILogger _logger = Log.ForContext<RabbitMQPubSubProvider>();

    private readonly PubSubOptions options;
    private readonly IAgentService agentService;

    private IModel? channel;

    private string? queueName;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentService"><see cref="IAgentService"/></param>
    /// <param name="options">The configuraion options</param>
    public RabbitMQPubSubProvider(IAgentService agentService, PubSubOptions options)
    {
        this.agentService = agentService;
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
    /// 
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
                    var agentInfoJson = Encoding.UTF8.GetString(body);
                    await this.agentService.RunAsync(agentInfoJson, cancellationToken);

                    _logger.Information(agentInfoJson);
                }

                channel.BasicAck(ea.DeliveryTag, false);
            };

            channel.BasicConsume(this.queueName, false, consumer);
        }
    }

    /// <summary>
    /// 
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