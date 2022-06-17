namespace ReconNessAgent.Domain.Core.Entities;

public partial class RootDomain : BaseEntity
{
    public RootDomain()
    {
        EventTracks = new HashSet<EventTrack>();
        Notes = new HashSet<Note>();
        Subdomains = new HashSet<Subdomain>();
    }

    public Guid Id { get; set; }
    public string? Name { get; set; }
    public Guid TargetId { get; set; }
    public bool HasBounty { get; set; }
    public string? AgentsRanBefore { get; set; }

    public virtual Target Target { get; set; } = null!;
    public virtual ICollection<EventTrack> EventTracks { get; set; }
    public virtual ICollection<Note> Notes { get; set; }
    public virtual ICollection<Subdomain> Subdomains { get; set; }
}
