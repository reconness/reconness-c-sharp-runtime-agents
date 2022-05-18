using Microsoft.Extensions.Hosting;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReconNessAgent.Infrastructure.Worker;

/// <summary>
/// This class extend the class <see cref="BackgroundService"/> to run on background as a service.
/// </summary>
public class Worker : BackgroundService
{
    private static readonly ILogger _logger = Log.ForContext<Worker>();

    private readonly IPubSubProvider pubSubProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Worker" /> class.
    /// </summary>
    /// <param name="pubSubProviderFactory"><see cref="IPubSubProviderFactory"/></param>
    public Worker(IPubSubProviderFactory pubSubProviderFactory)
    {
        this.pubSubProvider = pubSubProviderFactory.CreatePubSubProvider(PubSubType.RABBIT_MQ);
    }

    /// <summary>
    /// Execute the background service.
    /// </summary>
    /// <param name="stoppingToken">Notification that operations should be canceled.</param>
    /// <returns>A task.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {            
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.pubSubProvider.ConsumerAsync(stoppingToken);

            _logger.Information("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(10000, stoppingToken);
        }
    }
}

