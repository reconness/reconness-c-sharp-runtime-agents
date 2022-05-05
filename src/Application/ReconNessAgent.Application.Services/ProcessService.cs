using ReconNessAgent.Application;
using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Application.Services;

public class ProcessService : IProcessService
{
    private readonly IScriptEngineProvider scriptEngineProvider;

    public ProcessService(IScriptEngineProvider scriptEngineProvider)
    {
        this.scriptEngineProvider = scriptEngineProvider;
    }

    public Task ExecuteAsync(string agentInfoJson, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
