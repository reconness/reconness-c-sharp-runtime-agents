using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Domain.Core;
using ReconNessAgent.Domain.Core.Entities;
using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Application;

/// <summary>
/// This interface is used in all the interation with the database.
/// </summary>
public interface IAgentDataAccessService
{
    /// <summary>
    /// Obtain the <see cref="AgentRunner"/> entity base on the channel.
    /// </summary>
    /// <param name="channel">The channel for the search.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>The <see cref="AgentRunner"/> entity.</returns>
    Task<AgentRunner?> GetAgentRunnerAsync(IUnitOfWork unitOfWork, string channel, CancellationToken cancellationToken);

    /// <summary>
    /// Change the stage for the <see cref="AgentRunner"/> entity.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="agentRunner">The <see cref="AgentRunner"/> entity.</param>
    /// <param name="stage">The new stage <see cref="AgentRunnerStage"/>.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>A task.</returns>
    Task ChangeAgentRunnerStageAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, AgentRunnerStage stage, CancellationToken cancellationToken);

    /// <summary>
    /// Obtain the script from the Agent entity.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="agentRunner">The <see cref="AgentRunner"/> entity that contain the Agent Id foreign key <see cref="AgentRunner.AgentId"/>.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>The script to use to parse the terminal output.</returns>
    Task<string> GetAgentScriptAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, CancellationToken cancellationToken);

    /// <summary>
    /// Create an <see cref="AgentRunnerCommand"/> with the status RUNNING 
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="agentRunner">The <see cref="AgentRunner"/> entity.</param>
    /// <param name="agentRunnerQueue">The <see cref="AgentRunnerQueue"/> with the info from the queue.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>An <see cref="AgentRunnerCommand"/> with the status RUNNING created.</returns>
    Task<AgentRunnerCommand> CreateAgentRunnerCommandAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, AgentRunnerQueue agentRunnerQueue, CancellationToken cancellationToken);

    /// <summary>
    /// Save the output from the terminal.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="agentRunnerCommand">The <see cref="AgentRunnerCommand"/> entity to save with the output.</param>
    /// <param name="output">The output from the terminal to save.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>A Task</returns>
    Task SaveAgentRunnerCommandOutputAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, string output, CancellationToken cancellationToken);

    /// <summary>
    /// Change the <see cref="AgentRunnerCommand"/> status.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="agentRunnerCommand">the <see cref="AgentRunnerCommand"/>.</param>
    /// <param name="status">The new status, check <see cref="AgentRunnerCommandStatus"/>.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>A task</returns>
    Task ChangeAgentRunnerCommandStatusAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, AgentRunnerCommandStatus status, CancellationToken cancellationToken);

    /// <summary>
    /// Verify if we can skip the current command, because we ran the same command before.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="agentRunnerCommand">The <see cref="AgentRunnerCommand"/> entity</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>If we can skip the current command</returns>
    Task<bool> CanSkipAgentRunnerCommandAsync(IUnitOfWork unitOfWork, AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken);

    /// <summary>
    /// Save what we found parsing the terminal output using the agent script.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/></param>
    /// <param name="agentRunner">The <see cref="AgentRunner"/> entity.</param>
    /// <param name="outputParse">The script parsed.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>A task.</returns>
    Task SaveScriptOutputParseAsync(IUnitOfWork unitOfWork, AgentRunner agentRunner, TerminalOutputParse outputParse, CancellationToken cancellationToken);
}
