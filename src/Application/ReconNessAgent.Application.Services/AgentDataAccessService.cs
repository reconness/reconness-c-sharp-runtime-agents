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
    public async Task<AgentRunner?> GetAgentRunnerAsync(IUnitOfWork unitOfWork, string channel, CancellationToken cancellationToken)
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
    public async Task SaveScriptOutputParseAsync(IUnitOfWork unitOfWork, Channel channel, AgentRunner agentRunner, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        var agent = channel.Value.Agent;
        var target = channel.Value.Target;
        var rootDomain = channel.Value.RootDomain;
        var subdomain = channel.Value.Subdomain;

        // TODO: We are missing save targets!

        var agentTypeTarget = "Target".Equals(agent.AgentType);
        var agentTypeRootDomain = "RootDomain".Equals(agent.AgentType);
        var agentTypeSubdomain = "Subdomain".Equals(agent.AgentType);

        if (agentTypeTarget)
        {
            if (await this.NeedAddNewRootDomain(unitOfWork, target!, outputParse.RootDomain, cancellationToken))
            {
                await this.AddTargetNewRootDomainAsync(unitOfWork, target!, outputParse.RootDomain!, cancellationToken);
            }
        }
        else if (agentTypeRootDomain && rootDomain != null)
        {
            if (await this.NeedAddNewSubdomain(unitOfWork, rootDomain, outputParse.Subdomain, cancellationToken))
            {
                subdomain = await this.AddRootDomainNewSubdomainAsync(unitOfWork, rootDomain, outputParse.Subdomain!, agent!.Name!, cancellationToken);
            }
        }
        else if (agentTypeSubdomain && subdomain != null)
        {
            // if we have a new subdomain
            if (!string.IsNullOrEmpty(outputParse.Subdomain) && !outputParse.Subdomain.Equals(subdomain.Name) &&
                !await this.NeedAddNewSubdomain(unitOfWork, rootDomain, outputParse.Subdomain, cancellationToken))
            {
                await this.AddRootDomainNewSubdomainAsync(unitOfWork, rootDomain, outputParse.Subdomain!, agent!.Name!, cancellationToken);
            }

            if (!string.IsNullOrEmpty(outputParse.Ip))
            {
                await this.UpdateSubdomainIpAddressAsync(unitOfWork, subdomain, outputParse, cancellationToken);
            }

            if (outputParse.IsAlive != null)
            {
                await this.UpdateSubdomainIsAliveAsync(unitOfWork, subdomain, outputParse, cancellationToken);
            }

            if (outputParse.HasHttpOpen != null)
            {
                await this.UpdateSubdomainHasHttpOpenAsync(unitOfWork, subdomain, outputParse, cancellationToken);
            }

            if (outputParse.Takeover != null)
            {
                await this.UpdateSubdomainTakeoverAsync(unitOfWork, subdomain, outputParse, cancellationToken);
            }

            if (!string.IsNullOrEmpty(outputParse.HttpDirectory))
            {
                await this.UpdateSubdomainDirectoryAsync(unitOfWork, subdomain, outputParse, cancellationToken);
            }

            if (!string.IsNullOrEmpty(outputParse.Service))
            {
                await this.UpdateSubdomainServiceAsync(unitOfWork, subdomain, outputParse, cancellationToken);
            }

            if (!string.IsNullOrEmpty(outputParse.Technology))
            {
                await this.UpdateSubdomainTechnologyAsync(unitOfWork, subdomain, outputParse, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(outputParse.Label))
            {
                await this.UpdateSubdomainLabelAsync(unitOfWork, subdomain, outputParse, cancellationToken);
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
    private async ValueTask<bool> NeedAddNewRootDomain(IUnitOfWork unitOfWork, Target target, string? rootDomain, CancellationToken cancellationToken)
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
    private async Task<RootDomain> AddTargetNewRootDomainAsync(IUnitOfWork unitOfWork, Target target, string rootDomain, CancellationToken cancellationToken)
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
    private async ValueTask<bool> NeedAddNewSubdomain(IUnitOfWork unitOfWork, RootDomain rootDomain, string? subdomain, CancellationToken cancellationToken)
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
    /// <param name="target">The target</param>
    /// <param name="rootDomain">The root domain to add the new subdomain</param>
    /// <param name="subdomain">The new root domain</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>The new subdomain added</returns>
    private async Task<Subdomain> AddRootDomainNewSubdomainAsync(IUnitOfWork unitOfWork, RootDomain rootDomain, string subdomain, string agentName, CancellationToken cancellationToken)
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

    /// <summary>
    /// IP4 validation
    /// </summary>
    /// <param name="ipString">The Ip to validate</param>
    /// <returns>If is a valid IP</returns>
    private static bool ValidateIPv4(string ipString)
    {
        if (string.IsNullOrWhiteSpace(ipString) || ipString.Count(c => c == '.') != 3)
        {
            return false;
        }

        return IPAddress.TryParse(ipString, out IPAddress address);
    }

    /// <summary>
    /// Assign Ip address to the subdomain
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainIpAddressAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken = default)
    {
        if (ValidateIPv4(scriptOutput.Ip) && subdomain.IpAddress != scriptOutput.Ip)
        {
            subdomain.IpAddress = scriptOutput.Ip;
            unitOfWork.Repository<Subdomain>().Update(subdomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Update the subdomain if is Alive
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainIsAliveAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken = default)
    {
        if (subdomain.IsAlive != scriptOutput.IsAlive)
        {
            subdomain.IsAlive = scriptOutput.IsAlive.Value;
            unitOfWork.Repository<Subdomain>().Update(subdomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Update the subdomain if it has http port open
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainHasHttpOpenAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken = default)
    {
        if (subdomain.HasHttpOpen != scriptOutput.HasHttpOpen.Value)
        {
            subdomain.HasHttpOpen = scriptOutput.HasHttpOpen.Value;
            unitOfWork.Repository<Subdomain>().Update(subdomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Update the subdomain if it can be takeover
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainTakeoverAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken = default)
    {
        if (subdomain.Takeover != scriptOutput.Takeover.Value)
        {
            subdomain.Takeover = scriptOutput.Takeover.Value;
            unitOfWork.Repository<Subdomain>().Update(subdomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Update the subdomain with directory discovery
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainDirectoryAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken = default)
    {
        var httpDirectory = scriptOutput.HttpDirectory.TrimEnd('/').TrimEnd();
        if (subdomain.Directories == null)
        {
            subdomain.Directories = new List<Domain.Core.Entities.Directory>();
        }


        if (subdomain.Directories.Any(d => d.Uri == httpDirectory))
        {
            return;
        }

        var directory = new Domain.Core.Entities.Directory()
        {
            Uri = httpDirectory,
            StatusCode = scriptOutput.HttpDirectoryStatusCode,
            Method = scriptOutput.HttpDirectoryMethod,
            Size = scriptOutput.HttpDirectorySize
        };

        subdomain.Directories.Add(directory);
        unitOfWork.Repository<Subdomain>().Update(subdomain);
        await unitOfWork.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// Update the subdomain if is a new service with open port
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainServiceAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken = default)
    {
        if (subdomain.Services == null)
        {
            subdomain.Services = new List<Service>();
        }

        var service = new Service
        {
            Name = scriptOutput.Service.ToLower(),
            Port = scriptOutput.Port.Value
        };

        if (!subdomain.Services.Any(s => s.Name == service.Name && s.Port == service.Port))
        {
            subdomain.Services.Add(service);
            unitOfWork.Repository<Subdomain>().Update(subdomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Update the subdomain Technology
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainTechnologyAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(scriptOutput.Technology) && !scriptOutput.Technology.Equals(subdomain.Technology, StringComparison.OrdinalIgnoreCase))
        {
            subdomain.Technology = scriptOutput.Technology;
            unitOfWork.Repository<Subdomain>().Update(subdomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Update the subdomain label
    /// </summary>
    /// <param name="subdomain">The subdomain</param>
    /// <param name="scriptOutput">The terminal output one line</param>
    /// <param name="cancellationToken">Notification that operations should be canceled</param>
    /// <returns>A task</returns>
    private async ValueTask UpdateSubdomainLabelAsync(IUnitOfWork unitOfWork, Subdomain subdomain, TerminalOutputParse scriptOutput, CancellationToken cancellationToken = default)
    {
        if (!subdomain.Labels.Any(l => scriptOutput.Label.Equals(l.Name, StringComparison.OrdinalIgnoreCase)))
        {
            var label = await unitOfWork.Repository<Label>().GetByCriteriaAsync(l => l.Name.ToLower() == scriptOutput.Label.ToLower(), cancellationToken);
            if (label == null)
            {
                var random = new Random();
                label = new Label
                {
                    Name = scriptOutput.Label,
                    Color = string.Format("#{0:X6}", random.Next(0x1000000))
                };
            }

            subdomain.Labels.Add(label);

            unitOfWork.Repository<Subdomain>().Update(subdomain);
            await unitOfWork.CommitAsync(cancellationToken);
        }
    }
}
