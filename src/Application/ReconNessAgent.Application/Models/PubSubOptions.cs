namespace ReconNessAgent.Application.Models;

/// <summary>
/// This class define the connection string for the Pub/Sub provider
/// </summary>
public class PubSubOptions
{
    /// <summary>
    /// the Pub/Sub connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}
