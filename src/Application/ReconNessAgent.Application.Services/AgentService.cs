using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Domain.Core;
using ReconNessAgent.Domain.Core.Enums;
using System.Text.Json;

namespace ReconNessAgent.Application.Services;

/// <summary>
/// This class implement the interface <see cref="IAgentService"/> to allow run agent commands 
/// and save the data collected inside the database.
/// </summary>
public class AgentService : IAgentService
{
    private readonly IAgentDataAccessService agentDataAccessService;
    private readonly IScriptEngineProvideFactory scriptEngineProvideFactory;
    private readonly ITerminalProviderFactory terminalProviderFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentService" /> class.
    /// </summary>
    /// <param name="agentDataAccessService"><see cref="IAgentDataAccessService"/></param>
    /// <param name="scriptEngineProvideFactory"><see cref="IScriptEngineProvideFactory"/></param>
    /// <param name="terminalProviderFactory"><see cref="ITerminalProviderFactory"/></param>
    public AgentService(IAgentDataAccessService agentDataAccessService,
        IScriptEngineProvideFactory scriptEngineProvideFactory, 
        ITerminalProviderFactory terminalProviderFactory)
    {
        this.agentDataAccessService = agentDataAccessService;
        this.scriptEngineProvideFactory = scriptEngineProvideFactory;
        this.terminalProviderFactory = terminalProviderFactory;
    }

    /// <inheritdoc/>
    public async Task RunAsync(string agentRunnerQueueJson, CancellationToken cancellationToken = default)
    {
        var agentRunnerQueue = JsonSerializer.Deserialize<AgentRunnerQueue>(agentRunnerQueueJson);
        if (agentRunnerQueue != null)
        {
            var agentRunner = await this.agentDataAccessService.GetAgentRunnerAsync(agentRunnerQueue.Channel, cancellationToken);

            // change channel stage to RUNNING if the stage is ENQUEUE on AgentRunner
            if (agentRunner.Stage == AgentRunnerStage.ENQUEUE)
            {
                await this.agentDataAccessService.ChangeAgentRunnerStageAsync(agentRunner, AgentRunnerStage.RUNNING, cancellationToken);
            }

            // create agent runner command new entry with status RUNNING
            var agentRunnerCommand = await this.agentDataAccessService.CreateAgentRunnerCommandAsync(agentRunner, agentRunnerQueue, cancellationToken);

            // if we can skip this agent runner command, change status to SKIPPED
            if (agentRunner.AllowSkip && await this.agentDataAccessService.CanSkipAgentRunnerCommandAsync(agentRunnerCommand, cancellationToken))
            {
                await this.agentDataAccessService.ChangeAgentRunnerCommandStatusAsync(agentRunnerCommand, AgentRunnerCommandStatus.SKIPPED, cancellationToken);
                return;
            }
            
            await RunInternalAsync(agentRunnerQueue, agentRunner, agentRunnerCommand, cancellationToken);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunnerQueue"></param>
    /// <param name="agentRunner"></param>
    /// <param name="agentRunnerCommand"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task RunInternalAsync(AgentRunnerQueue agentRunnerQueue, AgentRunner agentRunner, AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken)
    {
        var terminalProvider = this.terminalProviderFactory.CreateTerminalProvider();

        AgentRunnerCommandStatus agentRunnerCommandStatus;

        try
        {
            // obtain the script from the Agent and the provider
            var script = await this.agentDataAccessService.GetAgentScriptAsync(agentRunner, cancellationToken);
            var scriptEngineProvider = this.scriptEngineProvideFactory.CreateScriptEngineProvider(script);

            agentRunnerCommandStatus = await RunTerminalAsync(agentRunnerQueue, agentRunner, agentRunnerCommand, terminalProvider, scriptEngineProvider, cancellationToken);
        }
        catch (Exception)
        {
            agentRunnerCommandStatus = AgentRunnerCommandStatus.FAILED;
        }

        await this.agentDataAccessService.ChangeAgentRunnerCommandStatusAsync(agentRunnerCommand, agentRunnerCommandStatus, cancellationToken);

        terminalProvider.Exit();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="agentRunnerQueue"></param>
    /// <param name="agentRunner"></param>
    /// <param name="agentRunnerCommand"></param>
    /// <param name="terminal"></param>
    /// <param name="scriptEngineProvider"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<AgentRunnerCommandStatus> RunTerminalAsync(AgentRunnerQueue agentRunnerQueue, AgentRunner agentRunner, AgentRunnerCommand agentRunnerCommand, ITerminalProvider terminal, IScriptEngineProvider scriptEngineProvider, CancellationToken cancellationToken)
    {
        var count = 1;
        terminal.Execute(agentRunnerQueue.Command);

        while (!terminal.Finished)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // check channel status if the agent stopped or failed break and stop the process on AgentRunner
            if (await this.agentDataAccessService.HasAgentRunnerStageAsync(agentRunner, new List<AgentRunnerStage> { AgentRunnerStage.STOPPED, AgentRunnerStage.FAILED }, cancellationToken))
            {
                return AgentRunnerCommandStatus.STOPPED;
            }

            var output = await terminal.ReadLineAsync();
            if (!string.IsNullOrEmpty(output))
            {
                var outputParse = await scriptEngineProvider.ParseAsync(output, count++, cancellationToken);

                // save terminalLineOutput on AgentRunnerOutput
                await this.agentDataAccessService.SaveAgentRunnerCommandOutputAsync(agentRunnerCommand, output, cancellationToken);

                // save what we parse from the terminal output
                await this.agentDataAccessService.SaveScriptOutputAsync(agentRunner, outputParse, cancellationToken);
            }
        }

        return AgentRunnerCommandStatus.SUCCESS;
    }
}
