namespace ReconNessAgent.Domain.Core.Entities;

public partial class AgentRunnerCommandOutput : BaseEntity
{
    public Guid Id { get; set; }
    public string? Output { get; set; }
    public Guid AgentRunnerCommandId { get; set; }
    public virtual AgentRunnerCommand AgentRunnerCommand { get; set; } = null!;
}
