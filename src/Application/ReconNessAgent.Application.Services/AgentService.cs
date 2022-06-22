using Microsoft.Extensions.DependencyInjection;
using ReconNessAgent.Application.DataAccess;
using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Domain.Core.Entities;
using ReconNessAgent.Domain.Core.Enums;
using ReconNessAgent.Domain.Core.ValueObjects;
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
                    var agentRunner = await this.agentDataAccessService.GetAgentRunnerAsync(unitOfWork, agentRunnerQueue.Channel, cancellationToken);
                    if (agentRunner == null)
                    {
                        return;
                    }

                    unitOfWork.BeginTransaction();

                    var channel = await FromChannelCompositionAsync(unitOfWork, agentRunnerQueue, cancellationToken);
                    
                    // change channel stage to RUNNING if the stage is ENQUEUE on AgentRunner
                    if (agentRunner.Stage == AgentRunnerStage.ENQUEUE)
                    {
                        await this.agentDataAccessService.ChangeAgentRunnerStageAsync(unitOfWork, agentRunner, AgentRunnerStage.RUNNING, cancellationToken);
                    }

                    // create agent runner command new entry with status RUNNING
                    var agentRunnerCommand = await this.agentDataAccessService.CreateAgentRunnerCommandAsync(unitOfWork, agentRunner, agentRunnerQueue, cancellationToken);

                    // if we can skip this agent runner command, change status to SKIPPED
                    if (agentRunner.AllowSkip && this.agentDataAccessService.CanSkipAgentRunnerCommand(channel, agentRunnerCommand))
                    {
                        await this.agentDataAccessService.ChangeAgentRunnerCommandStatusAsync(unitOfWork, agentRunnerCommand, AgentRunnerCommandStatus.SKIPPED, cancellationToken);
                        return;
                    }

                    await RunInternalAsync(unitOfWork, channel, agentRunnerQueue, agentRunner, agentRunnerCommand, cancellationToken);

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
    /// Decompose the channel into agent, target, rootdomain and subdomain
    /// 
    /// Ex:
    /// #20220319.1_nmap_yahoo_yahoo.com_www.yahoo.com
    /// #20220319.1_nmap_yahoo_yahoo.com_all
    /// </summary>
    /// <param name="channel">The channel</param>
    /// <param name="unitOfWork">The UnitOfWork</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>An agent, target, rootdomain and subdomain</returns>
    private async Task<Channel> FromChannelCompositionAsync(IUnitOfWork unitOfWork, AgentRunnerQueue agentRunnerQueue, CancellationToken cancellationToken)
    {
        var concepts = agentRunnerQueue.Channel.Split('_');

        var targetName = concepts[2];
        var agentName = concepts[1];

        var subdomainName = string.Empty;
        var rootdomainName = string.Empty;
        if (concepts.Length > 3)
        {
            rootdomainName = "all".Equals(concepts[3]) ? agentRunnerQueue .Payload : concepts[3];
        }

        if (concepts.Length > 4)
        {
            subdomainName = "all".Equals(concepts[4]) ? agentRunnerQueue.Payload : concepts[4];
        }

        Subdomain? subdomain = default;
        if (!string.IsNullOrEmpty(subdomainName))
        {
            subdomain = await unitOfWork.Repository<Subdomain>().GetByCriteriaAsync(s => s.Name == subdomainName, cancellationToken);
        }

        RootDomain? rootDomain = default;
        if (!string.IsNullOrEmpty(rootdomainName))
        {
            rootDomain = await unitOfWork.Repository<RootDomain>().GetByCriteriaAsync(s => s.Name == rootdomainName, cancellationToken);
        }

        var target = await unitOfWork.Repository<Target>().GetByCriteriaAsync(s => s.Name == targetName, cancellationToken);
        var agent = await unitOfWork.Repository<Agent>().GetByCriteriaAsync(s => s.Name == agentName, cancellationToken);

        return Channel.From((agent!, target!, rootDomain, subdomain));
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
    private async Task RunInternalAsync(IUnitOfWork unitOfWork, Channel channel, AgentRunnerQueue agentRunnerQueue, AgentRunner agentRunner, AgentRunnerCommand agentRunnerCommand, CancellationToken cancellationToken)
    {
        var terminalProvider = this.terminalProviderFactory.CreateTerminalProvider();

        AgentRunnerCommandStatus agentRunnerCommandStatus;

        try
        {
            // obtain the script from the Agent and the provider
            var script = await this.agentDataAccessService.GetAgentScriptAsync(unitOfWork, agentRunner, cancellationToken);
            if (string.IsNullOrEmpty(script))
            {
                return;
            }

            var scriptEngineProvider = this.scriptEngineProvideFactory.CreateScriptEngineProvider(script);

            agentRunnerCommandStatus = await RunTerminalAsync(unitOfWork, channel, agentRunnerQueue, agentRunner, agentRunnerCommand, terminalProvider, scriptEngineProvider, cancellationToken);
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
                                                                  Channel channel,
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
            if (new List<AgentRunnerStage> { AgentRunnerStage.STOPPED, AgentRunnerStage.FAILED }.Contains(agentRunner.Stage))
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
                await this.agentDataAccessService.SaveScriptOutputParseAsync(unitOfWork, channel, agentRunner, outputParse, cancellationToken);
            }
        }

        return AgentRunnerCommandStatus.SUCCESS;
    }
}
