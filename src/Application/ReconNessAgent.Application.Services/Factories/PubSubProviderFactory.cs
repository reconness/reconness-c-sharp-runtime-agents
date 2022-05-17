using Microsoft.Extensions.Options;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Infrastructure.PubSub;

namespace ReconNessAgent.Application.Services.Factories;

/// <summary>
/// This class implement the interface <see cref="IPubSubProviderFactory"/>, this class build a <see cref="IPubSubProvider"/> 
/// based on the type <see cref="PubSubType"/>, by default we use <see cref="PubSubType.RABBIT_MQ"/>.
/// </summary>
public class PubSubProviderFactory : IPubSubProviderFactory
{
    private readonly IAgentService agentService;
    private readonly PubSubOptions pubSubOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PubSubProviderFactory" /> class.
    /// </summary>
    /// <param name="agentService"><see cref="IAgentService"/></param>
    /// <param name="options"><see cref="IOptions{PubSubOptions}"/></param>
    public PubSubProviderFactory(IAgentService agentService, IOptions<PubSubOptions> options)
    {
        this.agentService = agentService;
        pubSubOptions = options.Value;
    }

    /// <inheritdoc/>
    public IPubSubProvider CreatePubSubProvider(PubSubType type = PubSubType.RABBIT_MQ)
    {
        return type switch
        {
            PubSubType.RABBIT_MQ => new RabbitMQPubSubProvider(agentService, pubSubOptions),
            _ => throw new ArgumentException(),
        };
    }
}
