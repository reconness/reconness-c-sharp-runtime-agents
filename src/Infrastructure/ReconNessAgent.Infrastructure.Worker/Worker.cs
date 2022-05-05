using Microsoft.Extensions.Hosting;
using ReconNessAgent.Application.Providers;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReconNessAgent.Infrastructure.Worker;

public class Worker : BackgroundService
{
    private static readonly ILogger _logger = Log.ForContext<Worker>();

    private readonly IPubSubProvider pubSubProvider;

    public Worker(IPubSubProvider pubSubProvider)
    {
        this.pubSubProvider = pubSubProvider;
    }

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

