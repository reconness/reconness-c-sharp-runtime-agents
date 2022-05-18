using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Domain.Core;

public class AgentRunner : BaseEntity
{
    public Guid Id { get; set; }

    public string Channel { get; set; } = String.Empty;

    public AgentRunnerStage Stage { get; set; }

    public int Total { get; set; }

    public bool AllowSkip { get; set; }

    public string AgentRunnerType { get; set; } = String.Empty;

    public bool ActivateNotification { get; set; }

    public Guid AgentId { get; set; }
}
