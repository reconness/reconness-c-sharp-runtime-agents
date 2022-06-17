namespace ReconNessAgent.Domain.Core.Entities;

public partial class Subdomain : BaseEntity
{
    public Subdomain()
    {
        Directories = new HashSet<Directory>();
        EventTracks = new HashSet<EventTrack>();
        Notes = new HashSet<Note>();
        Services = new HashSet<Service>();
        Labels = new HashSet<Label>();
    }

    public Guid Id { get; set; }
    public bool? HasHttpOpen { get; set; }
    public bool? IsMainPortal { get; set; }
    public string? Name { get; set; }
    public bool? IsAlive { get; set; }
    public string? IpAddress { get; set; }
    public bool? Takeover { get; set; }
    public Guid RootDomainId { get; set; }
    public string? Technology { get; set; }
    public bool? HasBounty { get; set; }
    public string? AgentsRanBefore { get; set; }

    public virtual RootDomain RootDomain { get; set; } = null!;
    public virtual ICollection<Directory> Directories { get; set; }
    public virtual ICollection<EventTrack> EventTracks { get; set; }
    public virtual ICollection<Note> Notes { get; set; }
    public virtual ICollection<Service> Services { get; set; }

    public virtual ICollection<Label> Labels { get; set; }
}
