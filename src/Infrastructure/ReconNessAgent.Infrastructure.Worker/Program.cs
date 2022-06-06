using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReconNessAgent.Application;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Services;
using ReconNessAgent.Application.Services.Factories;
using ReconNessAgent.Infrastructure.Data.EF;
using Serilog;
using System;

namespace ReconNessAgent.Infrastructure.Worker
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

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var configurationRoot = hostContext.Configuration;
                    services.Configure<PubSubOptions>(
                        configurationRoot.GetSection("PubSub"));

                    ConfigureDependencyInjection(services);

                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging((hostContext, builder) =>
                {
                    builder.ConfigureSerilog(hostContext.Configuration);
                })
                .UseSerilog();

        private static void ConfigureDependencyInjection(IServiceCollection services)
        {
            services.AddSingleton<IPubSubProviderFactory, PubSubProviderFactory>();
            services.AddSingleton<ITerminalProviderFactory, TerminalProviderFactory>();
            services.AddSingleton<IScriptEngineProvideFactory, ScriptEngineProvideFactory>();

            services.AddSingleton<IAgentService, AgentService>();
            services.AddSingleton<IAgentDataAccessService, AgentDataAccessService>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IDbContext, ReconnessDbContext>();            
        }
    }
}
