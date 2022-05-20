namespace ReconNessAgent.Application.Models;

public class AgentRunnerQueue
{
    public string Channel { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public int Count { get; set; }
    public int AvailableServerNumber { get; set; }
}
