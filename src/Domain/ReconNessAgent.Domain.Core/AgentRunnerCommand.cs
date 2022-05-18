using ReconNessAgent.Domain.Core.Enums;

namespace ReconNessAgent.Domain.Core;

public class AgentRunnerCommand : BaseEntity
{
    public Guid Id { get; set; }

    public AgentRunnerCommandStatus Status { get; set; }

    public string Command { get; set; } = String.Empty;

    public int Number { get; set; }

    public int Server { get; set; }

    public Guid AgentRunnerId { get; set; }
}
