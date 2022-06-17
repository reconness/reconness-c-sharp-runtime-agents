using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Domain.Core.Entities;

public partial class AgentRunner : BaseEntity
{
    public AgentRunner()
    {
        AgentRunnerCommands = new HashSet<AgentRunnerCommand>();
    }

    public Guid Id { get; set; }
    public string? Channel { get; set; }
    public AgentRunnerStage Stage { get; set; }
    public Guid AgentId { get; set; }
    public bool ActivateNotification { get; set; }
    public string? AgentRunnerType { get; set; }
    public bool AllowSkip { get; set; }
    public int Total { get; set; }

    public virtual Agent Agent { get; set; } = null!;
    public virtual ICollection<AgentRunnerCommand> AgentRunnerCommands { get; set; }
}
