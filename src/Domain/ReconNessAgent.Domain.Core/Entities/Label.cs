namespace ReconNessAgent.Domain.Core.Entities;

public partial class Label : BaseEntity
{
    public Label()
    {
        Subdomains = new HashSet<Subdomain>();
    }

    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }

    public virtual ICollection<Subdomain> Subdomains { get; set; }
}
