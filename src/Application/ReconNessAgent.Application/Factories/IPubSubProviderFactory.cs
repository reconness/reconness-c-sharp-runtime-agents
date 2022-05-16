using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Application.Factories;

public interface IPubSubProviderFactory
{
    IPubSubProvider CreatePubSubProvider(PubSubType type = PubSubType.RABBIT_MQ);
}
