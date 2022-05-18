namespace ReconNessAgent.Domain.Core;

public class AgentRunnerCommandOutput : BaseEntity
{
    public Guid Id { get; set; }

    public string Output { get; set; } = String.Empty;

    public Guid AgentRunnerCommandId { get; set; }
}
