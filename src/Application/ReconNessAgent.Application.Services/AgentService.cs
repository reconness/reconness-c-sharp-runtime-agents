using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models;
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
    /// Initializes a new instance of the <see cref="AgentService" /> class
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
            // change channel status to running if is queued on AgentRunner

            var terminal = this.terminalProviderFactory.CreateTerminalProvider();
            terminal.Execute(agentInfo.Command);
                        

            // obtain the script base in the channel on AgentRunner
            var script = string.Empty;
            var scriptEngineProvider = this.scriptEngineProvideFactory.CreateScriptEngineProvider(script);

            var count = 1;
            while (!terminal.Finished)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // check channel status if the agent stopped break and stop the process on AgentRunner

                var output = await terminal.ReadLineAsync();
                if (!string.IsNullOrEmpty(output))
                {
                    var scriptOutput = await scriptEngineProvider.ParseAsync(output, count++);

                    // save terminalLineOutput on AgentRunnerOutput
                    // update output tables
                }
            }

            terminal.Exit();
        }
    }
}
