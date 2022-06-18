using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Domain.Core.Entities;
using ReconNessAgent.Domain.Core.Enums;
using System.Text.RegularExpressions;

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
    public async Task<bool> CanSkipAgentRunnerCommandAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken)
    {
        var channel = agentRunnerCommand.AgentRunner.Channel;
        var (agent, target, rootDomain, subdomain) = await FromChannelAsync(channel, cancellationToken);

        return agentRunnerCommand.AgentRunner.CanSkip(agent, target, rootDomain, subdomain);
    }

    /// <inheritdoc/>
    public Task SaveScriptOutputParseAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private Task<(Agent, Target, RootDomain, Subdomain)> FromChannelAsync(string? channel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }    
}
