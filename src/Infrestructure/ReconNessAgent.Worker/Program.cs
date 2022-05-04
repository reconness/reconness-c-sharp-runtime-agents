using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReconNessAgent.Application;
using ReconNessAgent.Application.Models;
using ReconNessAgent.PubSub;
using Serilog;
using System;

namespace ReconNessAgent.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                CreateHostBuilder(args)
                    .Build()
                    .Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Worker service failed initiation. See exception for more details");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configurationRoot = hostContext.Configuration;
                    services.Configure<PubSubOptions>(
                        configurationRoot.GetSection("PubSub"));

                    services.AddSingleton<IPubSubProvider, RabbitMQPubSubProvider>();
                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging((hostContext, builder) =>
                {
                    builder.ConfigureSerilog(hostContext.Configuration);
                })
                .UseSerilog();
    }
}
