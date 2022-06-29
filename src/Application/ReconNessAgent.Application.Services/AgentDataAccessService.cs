using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Domain.Core.Entities;
using ReconNessAgent.Domain.Core.Enums;
using ReconNessAgent.Domain.Core.ValueObjects;
using System.Net;

namespace ReconNessAgent.Application.Services;

/// <summary>
/// This class implement the interface <see cref="IAgentDataAccessService"/> that provide access to the data layout through <see cref="IUnitOfWork"/>.
/// </summary>
public class AgentDataAccessService : IAgentDataAccessService
{
    /// <inheritdoc/>
    public async Task<AgentRunner?> GetAgentRunnerAsync(IUnitOfWork unitOfWork, string channel, CancellationToken cancellationToken = default)
    {
        return await unitOfWork.Repository<AgentRunner>().GetByCriteriaAsync(r => r.Channel == channel, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ChangeAgentRunnerStageAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, AgentRunnerStage stage, CancellationToken cancellationToken)
    {
        agentRunner.Stage = stage;
        unitOfWork.Repository<AgentRunner>().Update(agentRunner);

        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AgentRunnerCommand> CreateAgentRunnerCommandAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, AgentRunnerQueue agentRunnerQueue, CancellationToken cancellationToken)
    {
        var agentRunnerCommand = new AgentRunnerCommand
        {
            AgentRunner = agentRunner,
            Command = agentRunnerQueue.Command,
            Status = AgentRunnerCommandStatus.RUNNING,
            Number = agentRunnerQueue.Count,
            Server = agentRunnerQueue.AvailableServerNumber
        };

        unitOfWork.Repository<AgentRunnerCommand>().Add(agentRunnerCommand);
        await unitOfWork.CommitAsync(cancellationToken);

        return agentRunnerCommand;
    }

    /// <inheritdoc/>
    public async Task ChangeAgentRunnerCommandStatusAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, AgentRunnerCommandStatus status, CancellationToken cancellationToken)
    {
        agentRunnerCommand.Status = status;
        unitOfWork.Repository<AgentRunnerCommand>().Update(agentRunnerCommand);

        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> GetAgentScriptAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, CancellationToken cancellationToken)
    {        
        var agent = await unitOfWork.Repository<Agent>().GetByCriteriaAsync(a => a.Id == agentRunner.AgentId, cancellationToken);

        return agent?.Script ?? string.Empty;
    }
    
    /// <inheritdoc/>
    public async Task SaveAgentRunnerCommandOutputAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, string output, CancellationToken cancellationToken)
    {
        var agentRunnerCommandOutput = new AgentRunnerCommandOutput
        {
            AgentRunnerCommand = agentRunnerCommand,
            Output = output
        };

        unitOfWork.Repository<AgentRunnerCommandOutput>().Add(agentRunnerCommandOutput);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public bool CanSkipAgentRunnerCommand(Channel channel, AgentRunnerCommand agentRunnerCommand)
    {
        return agentRunnerCommand.AgentRunner.CanSkip(channel.Value.Agent, channel.Value.Target, channel.Value.RootDomain, channel.Value.Subdomain);
    }

    /// <inheritdoc/>
    public async Task SaveScriptOutputParseAsync(IUnitOfWork unitOfWork, Channel channel, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        var agent = channel.Value.Agent;

        var target = await AddOrGetTargetAsync(unitOfWork, channel.Value.Target, agent, outputParse, cancellationToken);
        var rootDomain = await AddOrGetRootDomainAsync(unitOfWork, target, channel.Value.RootDomain, agent, outputParse, cancellationToken);
        var subdomain = await AddNewSubdomainAsync(unitOfWork, rootDomain, agent, outputParse, cancellationToken);

        var subdomains = new List<Subdomain>();
        if (subdomain != null)
        {
            subdomains.Add(subdomain);
        }

        if (channel.Value.Subdomain != null)
        {
            subdomains.Add(channel.Value.Subdomain);
        }        

        await UpdateSubdomainsAsync(unitOfWork, subdomains, agent, outputParse, cancellationToken);
    }

    /// <summary>
    /// Add or obtain the target
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="target">The target</param>
    /// <param name="agent">The agent</param>
    /// <param name="outputParse">the output</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A target</returns>
    private static async Task<Target?> AddOrGetTargetAsync(IUnitOfWork unitOfWork, Target? target, Agent agent, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        if (target == null && await NeedAddNewTargetAsync(unitOfWork, outputParse.Target, cancellationToken))
        {
            target = new Target
            {
                Name = outputParse.Target,
                AgentsRanBefore = agent.Name
            };

            if (!string.IsNullOrEmpty(outputParse.Note))
            {
                target.AddNewNote(agent.Name!, outputParse.Note);               
            }

            unitOfWork.Repository<Target>().Add(target);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return target;
    }

    /// <summary>
    /// Add or get the rootdomain
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="target">The target</param>
    /// <param name="rootDomain">The rootdomain</param>
    /// <param name="agent">The agent</param>
    /// <param name="outputParse">the output</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A rootdomain</returns>
    private static async Task<RootDomain?> AddOrGetRootDomainAsync(IUnitOfWork unitOfWork, Target? target, RootDomain? rootDomain, Agent agent, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        if (target != null && rootDomain == null && await NeedAddNewRootDomainAsync(unitOfWork, target, outputParse.RootDomain, cancellationToken))
        {
            rootDomain = new RootDomain
            {
                Name = outputParse.RootDomain,
                AgentsRanBefore = agent.Name,
                Target = target
            };

            if (!string.IsNullOrEmpty(outputParse.Note))
            {
                rootDomain.AddNewNote(agent.Name!, outputParse.Note);
            }

            unitOfWork.Repository<RootDomain>().Add(rootDomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return rootDomain;
    }

    /// <summary>
    /// Add and get the new subdomain if was added
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="rootDomain">The rootdomain</param>
    /// <param name="agent">The agent</param>
    /// <param name="outputParse">the output</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A subdomain or null</returns>
    private static async Task<Subdomain?> AddNewSubdomainAsync(IUnitOfWork unitOfWork, RootDomain? rootDomain, Agent agent, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        if (rootDomain != null && await NeedAddNewSubdomainAsync(unitOfWork, rootDomain, outputParse.Subdomain, cancellationToken))
        {
            var newSubdomain = new Subdomain
            {
                Name = outputParse.Subdomain,
                AgentsRanBefore = agent.Name,
                RootDomain = rootDomain
            };

            unitOfWork.Repository<Subdomain>().Add(newSubdomain);
            await unitOfWork.CommitAsync(cancellationToken);

            return newSubdomain;
        }

        return null;
    }

    /// <summary>
    /// Update different properties of the subdomains
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="subdomains">The list if subdomains</param>
    /// <param name="agent">The agent</param>
    /// <param name="outputParse">the output</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    private static async Task UpdateSubdomainsAsync(IUnitOfWork unitOfWork, List<Subdomain> subdomains, Agent agent, TerminalOutputParse outputParse,  CancellationToken cancellationToken)
    {
        foreach (var subdomain in subdomains)
        {
            var subdomainDirty = false;

            if (!string.IsNullOrEmpty(outputParse.Ip))
            {
                subdomain.UpdateSubdomainIpAddress(outputParse.Ip);
                subdomainDirty = true;
            }

            if (outputParse.IsAlive != null)
            {
                subdomain.UpdateSubdomainIsAlive(outputParse.IsAlive.Value);
                subdomainDirty = true;
            }

            if (outputParse.HasHttpOpen != null)
            {
                subdomain.UpdateSubdomainHasHttpOpen(outputParse.HasHttpOpen.Value);
                subdomainDirty = true;
            }

            if (outputParse.Takeover != null)
            {
                subdomain.UpdateSubdomainTakeover(outputParse.Takeover.Value);
                subdomainDirty = true;
            }

            if (!string.IsNullOrEmpty(outputParse.HttpDirectory))
            {
                subdomain.UpdateSubdomainDirectory(outputParse.HttpDirectory, outputParse.HttpDirectoryStatusCode, outputParse.HttpDirectoryMethod, outputParse.HttpDirectorySize);
                subdomainDirty = true;
            }

            if (!string.IsNullOrEmpty(outputParse.Service))
            {
                subdomain.UpdateSubdomainService(outputParse.Service, outputParse.Port);
                subdomainDirty = true;
            }

            if (!string.IsNullOrEmpty(outputParse.Technology))
            {
                subdomain.UpdateSubdomainTechnology(outputParse.Technology);
                subdomainDirty = true;
            }

            if (!string.IsNullOrWhiteSpace(outputParse.Label))
            {
                var label = await unitOfWork.Repository<Label>().GetByCriteriaAsync(l => l.Name.ToLower() == outputParse.Label.ToLower(), cancellationToken);
                if (label == null)
                {
                    subdomain.UpdateSubdomainLabel(outputParse.Label);
                    subdomainDirty = true;
                }
            }

            if (!string.IsNullOrEmpty(outputParse.Note))
            {
                subdomain.AddNewNote(agent.Name!, outputParse.Note);
            }

            //TODO: save ExtraFields

            if (subdomainDirty)
            {
                unitOfWork.Repository<Subdomain>().Update(subdomain);
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// If we need to add a new target
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="target">The Target</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>If we need to add a new target</returns>
    private static async ValueTask<bool> NeedAddNewTargetAsync(IUnitOfWork unitOfWork, string? target, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(target))
        {
            return false;
        }

        var existtarget = await unitOfWork.Repository<Target>().AnyAsync(t => t.Name == target, cancellationToken);
        return !existtarget;
    }

    /// <summary>
    /// If we need to add a new RootDomain
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="target">The Target</param>
    /// <param name="rootDomain">The root domain</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>If we need to add a new RootDomain</returns>
    private static async ValueTask<bool> NeedAddNewRootDomainAsync(IUnitOfWork unitOfWork, Target target, string? rootDomain, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rootDomain))
        {
            return false;
        }

        var existRootDomain = await unitOfWork.Repository<RootDomain>().AnyAsync(r => r.Target == target && r.Name == rootDomain, cancellationToken);
        return !existRootDomain;
    }

    /// <summary>
    /// If we need to add a new subdomain
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="rootDomain">The root domain</param>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>If we need to add a new RootDomain</returns>
    private static async ValueTask<bool> NeedAddNewSubdomainAsync(IUnitOfWork unitOfWork, RootDomain rootDomain, string? subdomain, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(subdomain) || Uri.CheckHostName(subdomain) == UriHostNameType.Unknown)
        {
            return false;
        }

        var existSubdomain = await unitOfWork.Repository<Subdomain>().AnyAsync(s => s.RootDomain == rootDomain && s.Name == subdomain, cancellationToken);
        return !existSubdomain;
    }
}
