namespace ReconNessAgent.Domain.Core.Entities;

public partial class EventTrack : BaseEntity
{
    public Guid Id { get; set; }
    public string? Data { get; set; }
    public string? Username { get; set; }
    public Guid? AgentId { get; set; }
    public Guid? TargetId { get; set; }
    public Guid? RootDomainId { get; set; }
    public Guid? SubdomainId { get; set; }
    public virtual Agent? Agent { get; set; }
    public virtual RootDomain? RootDomain { get; set; }
    public virtual Subdomain? Subdomain { get; set; }
    public virtual Target? Target { get; set; }
}
