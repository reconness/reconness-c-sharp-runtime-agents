using ReconNessAgent.Application.Models;
using ReconNessAgent.Domain.Core;
using ReconNessAgent.Domain.Core.Enums;
using ReconNessAgent.Infrastructure.DataAccess;

namespace ReconNessAgent.Application.Services;

/// <summary>
/// This class implement the interface <see cref="IAgentDataAccessService"/> that provide access to the data layout through <see cref="IUnitOfWork"/>.
/// </summary>
public class AgentDataAccessService : IAgentDataAccessService
{
    private readonly IUnitOfWork unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDataAccessService" /> class.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    public AgentDataAccessService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public Task<bool> CanSkipAgentRunnerCommandAsync(AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task ChangeAgentRunnerCommandStatusAsync(AgentRunnerCommand agentRunnerCommand, AgentRunnerCommandStatus status, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task ChangeAgentRunnerStageAsync(AgentRunner agentRunner, AgentRunnerStage stage, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<AgentRunnerCommand> CreateAgentRunnerCommandAsync(AgentRunner agentRunner, AgentRunnerQueue agentRunnerQueue, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<AgentRunner> GetAgentRunnerAsync(string channel, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<string> GetAgentScriptAsync(AgentRunner agentRunner, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> HasAgentRunnerStageAsync(AgentRunner agentRunner, List<AgentRunnerStage> agentRunStages, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task SaveAgentRunnerCommandOutputAsync(AgentRunnerCommand agentRunnerCommand, string output, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task SaveScriptOutputAsync(AgentRunner agentRunner, TerminalOutputParse outputParse, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
