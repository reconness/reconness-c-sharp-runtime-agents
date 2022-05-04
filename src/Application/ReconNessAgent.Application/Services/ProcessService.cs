namespace ReconNessAgent.Application.Services
{
    public class ProcessService : IProcessService
    {
        public Task ExecuteAsync(string agentInfoJson, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
