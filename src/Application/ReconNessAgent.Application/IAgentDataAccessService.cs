using ReconNessAgent.Application.Models;
using ReconNessAgent.Domain.Core;
using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Application;

/// <summary>
/// This interface is used in all the interation with the database.
/// </summary>
public interface IAgentDataAccessService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<AgentRunner> GetAgentRunnerAsync(string channel, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunner"></param>
    /// <param name="stage"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task ChangeAgentRunnerStageAsync(AgentRunner agentRunner, AgentRunnerStage stage, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunner"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<string> GetAgentScriptAsync(AgentRunner agentRunner, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunner"></param>
    /// <param name="agentRunStages"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<bool> HasAgentRunnerStatusAsync(AgentRunner agentRunner, List<AgentRunnerStage> agentRunStages, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunnerCommand"></param>
    /// <param name="output"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task SaveAgentRunnerCommandOutputAsync(AgentRunnerCommand agentRunnerCommand, string output, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunner"></param>
    /// <param name="agentInfo"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<AgentRunnerCommand> CreateAgentRunnerCommandAsync(AgentRunner agentRunner, AgentRunnerQueue agentInfo, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunnerCommand"></param>
    /// <param name="status"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<AgentRunnerCommand> ChangeAgentRunnerCommandStatusAsync(AgentRunnerCommand agentRunnerCommand, AgentRunnerCommandStatus status, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunnerCommand"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<bool> CanSkipAgentRunnerCommandAsync(AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken);

    /// <summary>
    /// Save what we found parsing the terminal output
    /// </summary>
    /// <param name="agentRunner">The agent runner id</param>
    /// <param name="outputParse">The script parsed</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task SaveScriptOutputAsync(AgentRunner agentRunner, TerminalOutputParse outputParse, CancellationToken cancellationToken);
}
