using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReconNessAgent.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AgentRunnerQueueProvider agentRunnerQueueProvider;

        public Worker(ILogger<Worker> logger, IConfiguration configurable)
        {
            _logger = logger;
            this.agentRunnerQueueProvider = new AgentRunnerQueueProvider(configurable, logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {            
            while (!stoppingToken.IsCancellationRequested)
            {
                this.agentRunnerQueueProvider.Consumer();
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
