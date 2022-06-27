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
        var target = channel.Value.Target;
        var rootDomain = channel.Value.RootDomain;
        var subdomain = channel.Value.Subdomain;

        if (target != null && await NeedAddNewRootDomain(unitOfWork, target, outputParse.RootDomain, cancellationToken))
        {
            rootDomain = await AddNewRootDomainAsync(unitOfWork, target, outputParse.RootDomain!, cancellationToken);
        }
       
        if (rootDomain != null && await NeedAddNewSubdomain(unitOfWork, rootDomain, outputParse.Subdomain, cancellationToken))
        {
            subdomain = await AddNewSubdomainAsync(unitOfWork, rootDomain, outputParse.Subdomain!, agent!.Name!, cancellationToken);
        }

        if (subdomain != null)
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

            if (subdomainDirty)
            {
                unitOfWork.Repository<Subdomain>().Update(subdomain);
                await unitOfWork.CommitAsync(cancellationToken);
            }
        }
    }

    /// <summary>
    /// If we need to add a new RootDomain
    /// </summary>
    /// <param name="target">The Target</param>
    /// <param name="rootDomain">The root domain</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>If we need to add a new RootDomain</returns>
    private static async ValueTask<bool> NeedAddNewRootDomain(IUnitOfWork unitOfWork, Target target, string? rootDomain, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(rootDomain))
        {
            return false;
        }

        var existRootDomain = await unitOfWork.Repository<RootDomain>().AnyAsync(r => r.Target == target && r.Name == rootDomain, cancellationToken);
        return !existRootDomain;
    }

    /// <summary>
    /// Add a new root domain in the target
    /// </summary>
    /// <param name="target">The target to add the new root domain</param>
    /// <param name="rootDomain">The new root domain</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>The new root domain added</returns>
    private static async Task<RootDomain> AddNewRootDomainAsync(IUnitOfWork unitOfWork, Target target, string rootDomain, CancellationToken cancellationToken)
    {
        var newRootDomain = new RootDomain
        {
            Name = rootDomain,
            Target = target
        };

        unitOfWork.Repository<RootDomain>().Add(newRootDomain);
        await unitOfWork.CommitAsync(cancellationToken);

        return newRootDomain;
    }

    /// <summary>
    /// If we need to add a new subdomain
    /// </summary>
    /// <param name="rootDomain">The root domain</param>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>If we need to add a new RootDomain</returns>
    private static async ValueTask<bool> NeedAddNewSubdomain(IUnitOfWork unitOfWork, RootDomain rootDomain, string? subdomain, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(subdomain) || Uri.CheckHostName(subdomain) == UriHostNameType.Unknown)
        {
            return false;
        }

        var existSubdomain = await unitOfWork.Repository<Subdomain>().AnyAsync(s => s.RootDomain == rootDomain && s.Name == subdomain, cancellationToken);
        return !existSubdomain;
    }

    /// <summary>
    /// Add a new subdomain in the target
    /// </summary>
    /// <param name="rootDomain">The root domain to add the new subdomain</param>
    /// <param name="subdomain">The new root domain</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>The new subdomain added</returns>
    private static async Task<Subdomain> AddNewSubdomainAsync(IUnitOfWork unitOfWork, RootDomain rootDomain, string subdomain, string agentName, CancellationToken cancellationToken)
    {
        var newSubdomain = new Subdomain
        {
            Name = subdomain,
            AgentsRanBefore = agentName,
            RootDomain = rootDomain
        };

        unitOfWork.Repository<Subdomain>().Add(newSubdomain);
        await unitOfWork.CommitAsync(cancellationToken);

        return newSubdomain;
    }
}
