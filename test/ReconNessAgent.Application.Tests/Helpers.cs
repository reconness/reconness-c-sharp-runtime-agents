using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Domain.Core.Entities;

namespace ReconNessAgent.Application.Tests;

internal static class Helpers
{
    /// <summary>
    /// Create a mock test agent
    /// </summary>
    /// <returns>The agent</returns>
    internal static async Task<Agent> CreateTestAgentAsync(IUnitOfWork unitOfWork, string script, string command, string agentType, AgentTrigger? agentTrigger = default)
    {
        var agent = await unitOfWork.Repository<Agent>().GetByCriteriaAsync(a => a.Name == "TestAgentName");
        if (agent != null)
        {
            unitOfWork.Repository<Agent>().Delete(agent);
            await unitOfWork.CommitAsync();
        }

        agent = new Agent
        {
            Name = "TestAgentName",
            Command = command,
            AgentType = agentType,
            Script = script
        };

        if (agentTrigger != null)
        {
            agent.AgentTrigger = agentTrigger;
        }

        unitOfWork.Repository<Agent>().Add(agent);
        await unitOfWork.CommitAsync();

        return agent;
    }

    /// <summary>
    /// Create a mock test target
    /// </summary>
    /// <returns>The target</returns>
    internal static async Task<Target> CreateTestTargetAsync(IUnitOfWork unitOfWork, string agentName = "", bool hasBounty = false)
    {
        var target = await unitOfWork.Repository<Target>().GetByCriteriaAsync(a => a.Name == "TestTargetName");
        if (target != null)
        {
            unitOfWork.Repository<Target>().Delete(target);
            await unitOfWork.CommitAsync();
        }

        target = new Target
        {
            Name = "TestTargetName",
            HasBounty = hasBounty
        };

        if (!string.IsNullOrEmpty(agentName))
        {
            target.AgentsRanBefore = agentName;
        }

        unitOfWork.Repository<Target>().Add(target);
        await unitOfWork.CommitAsync();

        return target;
    }

    /// <summary>
    /// Create a mock test rootdomain
    /// </summary>
    /// <param name="target">The target that has a relation with the new rootdomain</param>
    /// <returns>The rootdomain</returns>
    internal static async Task<RootDomain> CreateTestRootDomainAsync(IUnitOfWork unitOfWork, Target target, string agentName = "", bool hasBounty = false)
    {
        var rootDomain = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(a => a.Name == "myrootdomain.com");
        if (rootDomain != null)
        {
            unitOfWork.Repository<RootDomain>().Delete(rootDomain);
            await unitOfWork.CommitAsync();
        }

        rootDomain = new RootDomain
        {
            Name = "myrootdomain.com",
            TargetId = target.Id,
            HasBounty = hasBounty
        };

        if (!string.IsNullOrEmpty(agentName))
        {
            rootDomain.AgentsRanBefore = agentName;
        }

        unitOfWork.Repository<RootDomain>().Add(rootDomain);
        await unitOfWork.CommitAsync();

        return rootDomain;
    }

    /// <summary>
    /// Create a mock test subdomain
    /// </summary>
    /// <param name="rootDomain">The rootdomain that has a relation with the new subdomain</param>
    /// <returns>The subdomain</returns>
    internal static async Task<Subdomain> CreateTestSubDomainAsync(IUnitOfWork unitOfWork, RootDomain rootDomain, string agentName = "", bool? hasBounty = default)
    {
        var subdomain = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(a => a.Name == "www.myrootdomain.com");
        if (subdomain != null)
        {
            unitOfWork.Repository<Subdomain>().Delete(subdomain);
            await unitOfWork.CommitAsync();
        }

        subdomain = new Subdomain
        {
            Name = "www.myrootdomain.com",
            RootDomainId = rootDomain.Id,
            HasBounty = hasBounty
        };

        if (!string.IsNullOrEmpty(agentName))
        {
            subdomain.AgentsRanBefore = agentName;
        }

        unitOfWork.Repository<Subdomain>().Add(subdomain);
        await unitOfWork.CommitAsync();

        return subdomain;
    }

    /// <summary>
    /// Create a mock test subdomain
    /// </summary>
    /// <param name="rootDomain">The rootdomain that has a relation with the new subdomain</param>
    /// <returns>The subdomain</returns>
    internal static async Task<IList<Subdomain>> CreateTestSubDomainsAsync(IUnitOfWork unitOfWork, RootDomain rootDomain, string agentName = "")
    {
        var subdomains = await unitOfWork.Repository<Subdomain>().GetAllByCriteriaAsync(a => a.Name!.EndsWith("myrootdomain.com"));
        if (subdomains != null)
        {
            unitOfWork.Repository<Subdomain>().DeleteRange(subdomains);
            await unitOfWork.CommitAsync();
        }

        subdomains = new List<Subdomain>
        {
            new Subdomain
            {
                Name = "www.myrootdomain.com",
                RootDomainId = rootDomain.Id,
                AgentsRanBefore = agentName
            },
            new Subdomain
            {
                Name = "www.myrootdomain1.com",
                RootDomainId = rootDomain.Id,
                AgentsRanBefore = agentName,
                HasHttpOpen = true,
                IsAlive = true,
                IsMainPortal = true
            }
        };

        unitOfWork.Repository<Subdomain>().AddRange(subdomains);
        await unitOfWork.CommitAsync();

        return subdomains;
    }

    /// <summary>
    /// Create an mock test AgentRunner
    /// </summary>
    /// <param name="agent">The agent</param>
    /// <param name="channel">The channel for that AgentRunner</param>
    /// <returns>The AgentRunner</returns>
    internal static async Task<AgentRunner> CreateAgentRunner(IUnitOfWork unitOfWork, Agent agent, string channel)
    {
        var agentRunner = await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(a => a.Channel == channel);
        if (agentRunner != null)
        {
            unitOfWork.Repository<AgentRunner>().Delete(agentRunner);
            await unitOfWork.CommitAsync();
        }

        agentRunner = new AgentRunner
        {
            Channel = channel,
            AllowSkip = true,
            Stage = Domain.Core.Enums.AgentRunnerStage.ENQUEUE,
            Total = 10,
            ActivateNotification = true,
            AgentId = agent.Id
        };

        unitOfWork.Repository<AgentRunner>().Add(agentRunner);
        await unitOfWork.CommitAsync();

        return agentRunner;
    }
}
