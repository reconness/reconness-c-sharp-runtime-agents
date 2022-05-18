using ReconNessAgent.Application.Models;
using ReconNessAgent.Domain.Core;
using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Application;

/// <summary>
/// This interface is used in all the interation with the database.
/// </summary>
public interface IAgentRepository
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
    /// <param name="runnerId"></param>
    /// <param name="stage"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task ChangeAgentRunnerStatusAsync(Guid runnerId, AgentRunnerStage stage, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<string> GetAgentScriptAsync(Guid agentId, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="runnerId"></param>
    /// <param name="agentRunStages"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<bool> HasAgentRunnerStatusAsync(Guid runnerId, List<AgentRunnerStage> agentRunStages, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandId"></param>
    /// <param name="output"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task SaveAgentRunnerCommandOutputAsync(Guid commandId, string output, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="runnerId"></param>
    /// <param name="agentInfo"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<AgentRunnerCommand> CreateAgentRunnerCommandAsync(Guid runnerId, AgentInfo agentInfo, CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="commandId"></param>
    /// <param name="status"></param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task<AgentRunnerCommand> UpdateAgentRunnerCommandAsync(Guid commandId, AgentRunnerCommandStatus status, CancellationToken cancellationToken);

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
    /// <param name="channel">The channel where we have the target, rootdomain, subdomain information</param>
    /// <param name="scriptOutput">The script parsed</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns></returns>
    Task SaveScriptOutputAsync(string channel, ScriptParse scriptOutput, CancellationToken cancellationToken);
}
