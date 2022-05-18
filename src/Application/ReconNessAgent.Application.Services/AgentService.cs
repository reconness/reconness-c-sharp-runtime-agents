using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Domain.Core.Enums;
using System.Text.Json;

namespace ReconNessAgent.Application.Services;

/// <summary>
/// This class implement the interface <see cref="IAgentService"/> to allow run agent commands 
/// and save the data collected inside the database.
/// </summary>
public class AgentService : IAgentService
{
    private readonly IAgentRepository agentRepository;
    private readonly IScriptEngineProvideFactory scriptEngineProvideFactory;
    private readonly ITerminalProviderFactory terminalProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentService" /> class.
    /// </summary>
    /// <param name="agentRepository"><see cref="IAgentRepository"/></param>
    /// <param name="scriptEngineProvideFactory"><see cref="IScriptEngineProvideFactory"/></param>
    /// <param name="terminalProviderFactory"><see cref="ITerminalProviderFactory"/></param>
    public AgentService(IAgentRepository agentRepository,
        IScriptEngineProvideFactory scriptEngineProvideFactory, 
        ITerminalProviderFactory terminalProviderFactory)
    {
        this.agentRepository = agentRepository;
        this.scriptEngineProvideFactory = scriptEngineProvideFactory;
        this.terminalProviderFactory = terminalProviderFactory;
    }

    /// <inheritdoc/>
    public async Task RunAsync(string agentInfoJson, CancellationToken cancellationToken = default)
    {
        var agentInfo = JsonSerializer.Deserialize<AgentInfo>(agentInfoJson);
        if (agentInfo != null)
        {
            var agentRunner = await this.agentRepository.GetAgentRunnerAsync(agentInfo.Channel, cancellationToken);
            // change channel status to running if is enqueue on AgentRunner
            if (agentRunner.Stage == AgentRunnerStage.ENQUEUE)
            {
                await this.agentRepository.ChangeAgentRunnerStatusAsync(agentRunner.Id, AgentRunnerStage.RUNNING, cancellationToken);
            }

            // create agent runner command entry
            var agentRunnerCommand = await this.agentRepository.CreateAgentRunnerCommandAsync(agentRunner.Id, agentInfo, cancellationToken);
            if (agentRunner.AllowSkip && await this.agentRepository.CanSkipAgentRunnerCommandAsync(agentRunnerCommand, cancellationToken))
            {
                await this.agentRepository.UpdateAgentRunnerCommandAsync(agentRunnerCommand.Id, AgentRunnerCommandStatus.SKIPPED, cancellationToken);
                return;
            }

            // obtain the script from the Agent
            var script = await this.agentRepository.GetAgentScriptAsync(agentRunner.AgentId, cancellationToken);
            var scriptEngineProvider = this.scriptEngineProvideFactory.CreateScriptEngineProvider(script);            

            var count = 1;

            var terminal = this.terminalProviderFactory.CreateTerminalProvider();
            terminal.Execute(agentInfo.Command);
            while (!terminal.Finished)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // check channel status if the agent stopped or failed break and stop the process on AgentRunner
                if (await this.agentRepository.HasAgentRunnerStatusAsync(agentRunner.Id, new List<AgentRunnerStage> { AgentRunnerStage.STOPPED, AgentRunnerStage.FAILED}, cancellationToken))
                {
                    agentRunnerCommand = await this.agentRepository.UpdateAgentRunnerCommandAsync(agentRunnerCommand.Id, AgentRunnerCommandStatus.STOPPED, cancellationToken);
                    break;
                }                

                var output = await terminal.ReadLineAsync();
                if (!string.IsNullOrEmpty(output))
                {
                    var scriptOutput = await scriptEngineProvider.ParseAsync(output, count++);

                    // save terminalLineOutput on AgentRunnerOutput
                    await this.agentRepository.SaveAgentRunnerCommandOutputAsync(agentRunnerCommand.Id, output, cancellationToken);
                    await this.agentRepository.SaveScriptOutputAsync(agentInfo.Channel, scriptOutput, cancellationToken);
                }
            }

            if (agentRunnerCommand.Status == AgentRunnerCommandStatus.RUNNING)
            {
                await this.agentRepository.UpdateAgentRunnerCommandAsync(agentRunnerCommand.Id, AgentRunnerCommandStatus.SUCCESS, cancellationToken);
            }

            terminal.Exit();
        }
    }
}
