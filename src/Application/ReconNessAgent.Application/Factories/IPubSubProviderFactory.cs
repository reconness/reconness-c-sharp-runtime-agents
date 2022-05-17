using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Application.Factories;

/// <summary>
/// This interface expose a method to build a <see cref="IPubSubProvider"/> 
/// based on the type <see cref="PubSubType"/>, by default we use <see cref="PubSubType.RABBIT_MQ"/>
/// </summary>
public interface IPubSubProviderFactory
{
    /// <summary>
    /// A method to build a <see cref="IPubSubProvider"/> based on the type <see cref="PubSubType"/>, by default we use <see cref="PubSubType.RABBIT_MQ"/>
    /// </summary>
    /// <param name="type">The pub/sub provider type, by default we use <see cref="PubSubType.RABBIT_MQ"/></param>
    /// <returns>A <see cref="IPubSubProvider"/></returns>
    /// <exception cref="ArgumentException">if the pub/sub type does not exist</exception>
    IPubSubProvider CreatePubSubProvider(PubSubType type = PubSubType.RABBIT_MQ);
}
