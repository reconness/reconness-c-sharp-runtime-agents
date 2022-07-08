using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Domain.Core.Entities;

public partial class AgentRunner : BaseEntity
{
    public AgentRunner()
    {
        AgentRunnerCommands = new HashSet<AgentRunnerCommand>();
    }

    public Guid Id { get; set; }
    public string? Channel { get; set; }
    public AgentRunnerStage Stage { get; set; }
    public Guid AgentId { get; set; }
    public bool ActivateNotification { get; set; }
    public bool AllowSkip { get; set; }
    public int Total { get; set; }

    public virtual Agent Agent { get; set; } = null!;
    public virtual ICollection<AgentRunnerCommand> AgentRunnerCommands { get; set; }

    /// <summary>
    /// Check if we need to skip the subdomain and does not the agent in that subdomain
    /// </summary>
    /// <param name="agentRunner">The agent runner</param>
    /// <param name="agentRunnerType">The agent runner type</param>
    public bool CanSkip(Agent agent, Target? target, RootDomain? rootDomain, Subdomain? subdomain)
    {
        if (agent == null || agent.AgentTrigger == null)
        {
            return false;
        }

        var agentTrigger = agent.AgentTrigger;

        var agentTypeTarget = "Target".Equals(agent.AgentType);
        var agentTypeRootDomain = "RootDomain".Equals(agent.AgentType);
        var agentTypeSubdomain = "Subdomain".Equals(agent.AgentType);

        return (agentTrigger.SkipIfRunBefore ?? false && RanBefore(agent, target, rootDomain, subdomain, agentTypeTarget, agentTypeRootDomain, agentTypeSubdomain)) ||
               (agentTypeTarget && target != null && target.CanSkip(agentTrigger)) ||
               (agentTypeRootDomain && rootDomain != null && rootDomain.CanSkip(agentTrigger)) ||
               (agentTypeSubdomain && subdomain != null && subdomain.CanSkip(agentTrigger));
    }

    /// <summary>
    /// If ran before (target, rootdomain, subdomain)
    /// </summary>
    /// <param name="agentTypeTarget">if is the Target the agent type</param>
    /// <param name="agentTypeRootDomain">if is the RootDomain the agent type</param>
    /// <param name="agentTypeSubdomain">if is the Subdomain the agent type</param>
    /// <returns>If ran before (target, rootdomain, subdomain)></returns>
    private bool RanBefore(Agent agent, Target? target, RootDomain? rootDomain, Subdomain? subdomain, bool agentTypeTarget, bool agentTypeRootDomain, bool agentTypeSubdomain)
    {
        var agentRanBeforeInThisTarget = agentTypeTarget && target != null &&
                                             !string.IsNullOrEmpty(target.AgentsRanBefore) &&
                                             target.AgentsRanBefore.Contains(agent.Name!);

        var agentRanBeforeInThisRootDomain = agentTypeRootDomain && rootDomain != null &&
                                             !string.IsNullOrEmpty(rootDomain.AgentsRanBefore) &&
                                             rootDomain.AgentsRanBefore.Contains(agent.Name!);

        var agentRanBeforeInThisSubdomain = agentTypeSubdomain && subdomain != null &&
                                             !string.IsNullOrEmpty(subdomain.AgentsRanBefore) &&
                                             subdomain.AgentsRanBefore.Contains(agent.Name!);

        return agentRanBeforeInThisTarget || agentRanBeforeInThisRootDomain || agentRanBeforeInThisSubdomain;
    }
}
