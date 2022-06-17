namespace ReconNessAgent.Domain.Core.Entities;

public partial class Service : BaseEntity
{
    public Guid Id { get; set; }
    public int Port { get; set; }
    public Guid SubdomainId { get; set; }
    public string? Name { get; set; }

    public virtual Subdomain Subdomain { get; set; } = null!;
}
