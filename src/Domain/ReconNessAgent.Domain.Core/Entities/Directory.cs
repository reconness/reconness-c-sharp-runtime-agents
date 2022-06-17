namespace ReconNessAgent.Domain.Core.Entities;

public partial class Directory : BaseEntity
{
    public Guid Id { get; set; }
    public string? Uri { get; set; }
    public string? StatusCode { get; set; }
    public string? Size { get; set; }
    public string? Method { get; set; }
    public Guid? SubdomainId { get; set; }

    public virtual Subdomain? Subdomain { get; set; }
}
