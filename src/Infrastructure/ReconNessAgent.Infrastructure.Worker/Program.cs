using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

                    ConfigureDependencyInjection(services, configurationRoot, hostContext.HostingEnvironment);

                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging((hostContext, builder) =>
                {
                    builder.ConfigureSerilog(hostContext.Configuration);
                })
                .UseSerilog();

        private static void ConfigureDependencyInjection(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            services.AddSingleton<IPubSubProviderFactory, PubSubProviderFactory>();
            services.AddSingleton<ITerminalProviderFactory, TerminalProviderFactory>();
            services.AddSingleton<IScriptEngineProvideFactory, ScriptEngineProvideFactory>();

            services.AddSingleton<IAgentService, AgentService>();
            services.AddSingleton<IAgentDataAccessService, AgentDataAccessService>();

            var connectionString = GetConnectionString(configuration, env);

            var optionsBuilder = new DbContextOptionsBuilder<ReconnessDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            services.AddScoped<IDbContext>(d => new ReconnessDbContext(optionsBuilder.Options));
            services.AddScoped<IUnitOfWork, UnitOfWork>();           
        }

        private static string GetConnectionString(IConfiguration configuration, IHostEnvironment env)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (!"Development".Equals(env.EnvironmentName))
            {
                var pgDatabase = Environment.GetEnvironmentVariable("PostgresDb") ??
                                 Environment.GetEnvironmentVariable("PostgresDb", EnvironmentVariableTarget.User);
                var pgUserName = Environment.GetEnvironmentVariable("PostgresUser") ??
                                 Environment.GetEnvironmentVariable("PostgresUser", EnvironmentVariableTarget.User);
                var pgpassword = Environment.GetEnvironmentVariable("PostgresPassword") ??
                                 Environment.GetEnvironmentVariable("PostgresPassword", EnvironmentVariableTarget.User);

                connectionString = connectionString.Replace("{{database}}", pgDatabase)
                                                   .Replace("{{username}}", pgUserName)
                                                   .Replace("{{password}}", pgpassword);
            }

            return connectionString;
        }
    }
}
