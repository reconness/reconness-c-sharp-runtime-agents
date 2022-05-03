using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReconNessAgent.Application;
using ReconNessAgent.Application.Models;
using ReconNessAgent.PubSub;

namespace ReconNessAgent.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configurationRoot = hostContext.Configuration;
                    services.Configure<PubSubOptions>(
                        configurationRoot.GetSection("PubSub"));

                    services.AddScoped<IPubSubProvider, RabbitMQPubSubProvider>();
                    services.AddHostedService<Worker>();
                });
    }
}
