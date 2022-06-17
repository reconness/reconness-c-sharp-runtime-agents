namespace ReconNessAgent.Domain.Core.Entities;

public partial class AgentsSetting : BaseEntity
{
    public Guid Id { get; set; }
    public int Strategy { get; set; }
    public int AgentServerCount { get; set; }
}
