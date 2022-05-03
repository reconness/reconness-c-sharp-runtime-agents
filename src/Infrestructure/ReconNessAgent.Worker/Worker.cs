using Microsoft.Extensions.Hosting;
using NLog;
using ReconNessAgent.Application;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReconNessAgent.Worker
{
    public class Worker : BackgroundService
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IPubSubProvider pubSubProvider;

        public Worker(IPubSubProvider pubSubProvider)
        {
            this.pubSubProvider = pubSubProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            while (!stoppingToken.IsCancellationRequested)
            {
                this.pubSubProvider.Consumer();

                _logger.Info("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(10000, stoppingToken);
            }
        }
    }
}
