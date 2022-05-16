namespace ReconNessAgent.Application;

public interface IAgentService
{
    public Task RunAsync(string agentInfoJson, CancellationToken cancellationToken = default);
}
