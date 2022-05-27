using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Domain.Core;
using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Application.Services;

/// <summary>
/// This class implement the interface <see cref="IAgentDataAccessService"/> that provide access to the data layout through <see cref="IUnitOfWork"/>.
/// </summary>
public class AgentDataAccessService : IAgentDataAccessService
{
    /// <inheritdoc/>
    public Task<bool> CanSkipAgentRunnerCommandAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task ChangeAgentRunnerCommandStatusAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, AgentRunnerCommandStatus status, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task ChangeAgentRunnerStageAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, AgentRunnerStage stage, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<AgentRunnerCommand> CreateAgentRunnerCommandAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, AgentRunnerQueue agentRunnerQueue, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<AgentRunner> GetAgentRunnerAsync(IUnitOfWork unitOfWork, string channel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<string> GetAgentScriptAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> HasAgentRunnerStageAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, List<AgentRunnerStage> agentRunStages, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task SaveAgentRunnerCommandOutputAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, string output, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task SaveScriptOutputAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
