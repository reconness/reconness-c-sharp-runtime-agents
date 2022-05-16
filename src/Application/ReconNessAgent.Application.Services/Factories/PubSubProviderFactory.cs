using Microsoft.Extensions.Options;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Infrastructure.PubSub;

namespace ReconNessAgent.Application.Services.Factories;

public class PubSubProviderFactory : IPubSubProviderFactory
{
    private readonly IAgentService agentService;
    private readonly PubSubOptions pubSubOptions;

    public PubSubProviderFactory(IAgentService agentService, IOptions<PubSubOptions> options)
    {
        this.agentService = agentService;
        pubSubOptions = options.Value;
    }

    public IPubSubProvider CreatePubSubProvider(PubSubType type = PubSubType.RABBIT_MQ)
    {
        return type switch
        {
            PubSubType.RABBIT_MQ => new RabbitMQPubSubProvider(agentService, pubSubOptions),
            _ => throw new ArgumentException(),
        };
    }
}
