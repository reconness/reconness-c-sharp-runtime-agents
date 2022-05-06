using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using System.Text.Json;

namespace ReconNessAgent.Application.Services;

public class AgentService : IAgentService
{
    private readonly IScriptEngineProvider scriptEngineProvider;
    private readonly IProcessProviderFactory processProviderFactory;

    public AgentService(IScriptEngineProvider scriptEngineProvider, IProcessProviderFactory processProviderFactory)
    {
        this.scriptEngineProvider = scriptEngineProvider;
        this.processProviderFactory = processProviderFactory;
    }

    public async Task RunAsync(string agentInfoJson, CancellationToken cancellationToken = default)
    {
        var agentInfo = JsonSerializer.Deserialize<AgentInfo>(agentInfoJson);
        if (agentInfo != null)
        {
            // change channel status to running if is queued on AgentRunner

            var process = this.processProviderFactory.Build();
            process.Start(agentInfo.Command);

            var lineCount = 1;

            // obtain the script base in the channel on AgentRunner
            var script = string.Empty;

            while (!process.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // check channel status if the agent stopped break and stop the process on AgentRunner

                var terminalLineOutput = process.ReadLine();
                if (!string.IsNullOrEmpty(terminalLineOutput))
                {
                    var scriptOutput = await this.scriptEngineProvider.ParseAsync(script, terminalLineOutput, lineCount++);

                    // save terminalLineOutput on AgentRunnerOutput
                    // update output tables
                }
            }

            process.Stop();
        }
    }
}
