namespace ReconNessAgent.Application
{
    public interface IProcessService
    {
        public Task ExecuteAsync(string agentInfoJson, CancellationToken cancellationToken = default);
    }
}
