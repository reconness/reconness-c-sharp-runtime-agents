namespace ReconNessAgent.Application.Providers;

/// <summary>
/// This interface expose the method to consume message from the pub/sub provider that we are using.
/// </summary>
public interface IPubSubProvider
{
    /// <summary>
    /// This method register the event to check if there is a new message to consume.
    /// </summary>
    /// <param name="stoppingToken">Notification that operations should be canceled.</param>
    /// <returns>A task.</returns>
    Task ConsumerAsync(CancellationToken stoppingToken);
}

