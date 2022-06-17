namespace ReconNessAgent.Domain.Core.Entities;

public partial class Category : BaseEntity
{
    public Category()
    {
        Agents = new HashSet<Agent>();
    }

    public Guid Id { get; set; }
    public string? Name { get; set; }

    public virtual ICollection<Agent> Agents { get; set; }
}
