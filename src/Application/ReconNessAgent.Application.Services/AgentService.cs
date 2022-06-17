using Microsoft.Extensions.DependencyInjection;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Domain.Core.Entities;
using ReconNessAgent.Domain.Core.Enums;
using Serilog;
using System.Text.Json;

namespace ReconNessAgent.Application.Services;

/// <summary>
/// This class implement the interface <see cref="IAgentService"/> to allow run agent commands 
/// and save the data collected inside the database.
/// </summary>
public class AgentService : IAgentService
{
    private static readonly ILogger _logger = Log.ForContext<AgentService>();

    private readonly IAgentDataAccessService agentDataAccessService;
    private readonly IScriptEngineProvideFactory scriptEngineProvideFactory;
    private readonly ITerminalProviderFactory terminalProviderFactory;

    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentService" /> class.
    /// </summary>
    /// <param name="agentDataAccessService"><see cref="IAgentDataAccessService"/></param>
    /// <param name="scriptEngineProvideFactory"><see cref="IScriptEngineProvideFactory"/></param>
    /// <param name="terminalProviderFactory"><see cref="ITerminalProviderFactory"/></param>
    /// <param name="serviceProvid"></param>
    public AgentService(
        IAgentDataAccessService agentDataAccessService,
        IScriptEngineProvideFactory scriptEngineProvideFactory, 
        ITerminalProviderFactory terminalProviderFactory,
        IServiceProvider serviceProvider)
    {
        this.agentDataAccessService = agentDataAccessService;
        this.scriptEngineProvideFactory = scriptEngineProvideFactory;
        this.terminalProviderFactory = terminalProviderFactory;
        this.serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task RunAsync(string agentRunnerQueueJson, CancellationToken cancellationToken = default)
    {
        var agentRunnerQueue = JsonSerializer.Deserialize<AgentRunnerQueue>(agentRunnerQueueJson);
        if (agentRunnerQueue != null)
        {
            using var scope = this.serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();
            if (unitOfWork != null)
            {
                try
                {
                    unitOfWork.BeginTransaction();
                    var agentRunner = await this.agentDataAccessService.GetAgentRunnerAsync(unitOfWork, agentRunnerQueue.Channel, cancellationToken);

                    // change channel stage to RUNNING if the stage is ENQUEUE on AgentRunner
                    if (agentRunner.Stage == AgentRunnerStage.ENQUEUE)
                    {
                        await this.agentDataAccessService.ChangeAgentRunnerStageAsync(unitOfWork, agentRunner, AgentRunnerStage.RUNNING, cancellationToken);
                    }

                    // create agent runner command new entry with status RUNNING
                    var agentRunnerCommand = await this.agentDataAccessService.CreateAgentRunnerCommandAsync(unitOfWork, agentRunner, agentRunnerQueue, cancellationToken);

                    // if we can skip this agent runner command, change status to SKIPPED
                    if (agentRunner.AllowSkip && await this.agentDataAccessService.CanSkipAgentRunnerCommandAsync(unitOfWork, agentRunnerCommand, cancellationToken))
                    {
                        await this.agentDataAccessService.ChangeAgentRunnerCommandStatusAsync(unitOfWork, agentRunnerCommand, AgentRunnerCommandStatus.SKIPPED, cancellationToken);
                        return;
                    }

                    await RunInternalAsync(unitOfWork, agentRunnerQueue, agentRunner, agentRunnerCommand, cancellationToken);

                    await unitOfWork.CommitAsync();
                }
                catch (Exception)
                {
                    unitOfWork.Rollback();
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="agentRunnerQueue"></param>
    /// <param name="agentRunner"></param>
    /// <param name="agentRunnerCommand"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task RunInternalAsync(IUnitOfWork unitOfWork, AgentRunnerQueue agentRunnerQueue, AgentRunner agentRunner, AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken)
    {
        var terminalProvider = this.terminalProviderFactory.CreateTerminalProvider();

        AgentRunnerCommandStatus agentRunnerCommandStatus;

        try
        {
            // obtain the script from the Agent and the provider
            var script = await this.agentDataAccessService.GetAgentScriptAsync(unitOfWork, agentRunner, cancellationToken);
            var scriptEngineProvider = this.scriptEngineProvideFactory.CreateScriptEngineProvider(script);

            agentRunnerCommandStatus = await RunTerminalAsync(unitOfWork, agentRunnerQueue, agentRunner, agentRunnerCommand, terminalProvider, scriptEngineProvider, cancellationToken);
        }
        catch (Exception)
        {
            agentRunnerCommandStatus = AgentRunnerCommandStatus.FAILED;
        }

        await this.agentDataAccessService.ChangeAgentRunnerCommandStatusAsync(unitOfWork, agentRunnerCommand, agentRunnerCommandStatus, cancellationToken);

        terminalProvider.Exit();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="agentRunnerQueue"></param>
    /// <param name="agentRunner"></param>
    /// <param name="agentRunnerCommand"></param>
    /// <param name="terminal"></param>
    /// <param name="scriptEngineProvider"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<AgentRunnerCommandStatus> RunTerminalAsync(IUnitOfWork unitOfWork,
                                                                  AgentRunnerQueue agentRunnerQueue,
                                                                  AgentRunner agentRunner,
                                                                  AgentRunnerCommand agentRunnerCommand,
                                                                  ITerminalProvider terminal,
                                                                  IScriptEngineProvider scriptEngineProvider,
                                                                  CancellationToken cancellationToken)
    {
        var count = 1;
        terminal.Execute(agentRunnerQueue.Command);

        while (!terminal.Finished)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // check channel status if the agent stopped or failed break and stop the process on AgentRunner
            if (await this.agentDataAccessService.HasAgentRunnerStageAsync(unitOfWork, agentRunner, new List<AgentRunnerStage> { AgentRunnerStage.STOPPED, AgentRunnerStage.FAILED }, cancellationToken))
            {
                return AgentRunnerCommandStatus.STOPPED;
            }

            var output = await terminal.ReadLineAsync();
            if (!string.IsNullOrEmpty(output))
            {
                var outputParse = await scriptEngineProvider.ParseAsync(output, count++, cancellationToken);

                // save terminalLineOutput on AgentRunnerOutput
                await this.agentDataAccessService.SaveAgentRunnerCommandOutputAsync(unitOfWork, agentRunnerCommand, output, cancellationToken);

                // save what we parse from the terminal output
                await this.agentDataAccessService.SaveScriptOutputAsync(unitOfWork, agentRunner, outputParse, cancellationToken);
            }
        }

        return AgentRunnerCommandStatus.SUCCESS;
    }
}
