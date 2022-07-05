using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ReconNessAgent.Infrastructure.Worker
{
    /// <summary>
    /// This class is to extend the <see cref="ILoggingBuilder"/> using Serilog to read the cofiguration from the configuration file.
    /// </summary>
    public static class LoggingBuilderExtensions
    {
        /// <summary>
        /// This method extend <see cref="ILoggingBuilder"/> to configure Serilog using the configuration file.
        /// </summary>
        /// <param name="loggingBuilder">The <see cref="ILoggingBuilder"/> to exted.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        /// <returns>The <see cref="ILoggingBuilder"/>.</returns>
        public static ILoggingBuilder ConfigureSerilog(this ILoggingBuilder loggingBuilder, IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            return loggingBuilder;
        }
    }
}
