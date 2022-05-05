namespace ReconNessAgent.Application.Providers
{
    public interface IPubSubProvider
    {
        Task ConsumerAsync(CancellationToken stoppingToken);
    }
}
