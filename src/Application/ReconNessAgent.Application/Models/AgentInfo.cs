namespace ReconNessAgent.Application.Models;

public class AgentInfo
{
    public string Channel { get; set; }
    public string Command { get; set; }
    public string AgentRunnerType { get; set; }
    public bool Last { get; set; }
    public bool AllowSkip { get; set; }
    public int Count { get; set; }
    public int Total { get; set; }
    public int AvailableServerNumber { get; set; }
}
