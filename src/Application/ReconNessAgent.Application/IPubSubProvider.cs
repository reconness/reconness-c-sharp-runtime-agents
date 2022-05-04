namespace ReconNessAgent.Application
{
    public interface IPubSubProvider
    {
        Task ConsumerAsync(CancellationToken stoppingToken);
    }
}
