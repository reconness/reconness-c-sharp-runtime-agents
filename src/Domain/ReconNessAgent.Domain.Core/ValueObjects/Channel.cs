using ReconNessAgent.Domain.Core.Entities;
using ValueOf;

namespace ReconNessAgent.Domain.Core.ValueObjects;

public class Channel : ValueOf<(Agent Agent, Target Target, RootDomain? RootDomain, Subdomain? Subdomain), Channel>
{
}
