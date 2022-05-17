namespace ReconNessAgent.Application;

/// <summary>
/// This interface provide the entry point to run the agent command in the terminal and save the result directly into the database using the channel as Id.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// This method receive the agent information serialize on json <see cref="agentInfoJson"/> with the data to run the command in the terminal
    /// and save the output into the database using a channel format.
    /// </summary>
    /// <param name="agentInfoJson">The agent information serialize on json.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>A Task.</returns>
    public Task RunAsync(string agentInfoJson, CancellationToken cancellationToken = default);
}
