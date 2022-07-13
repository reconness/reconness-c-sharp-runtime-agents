using System.Text.RegularExpressions;

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

    /// <summary>
    /// If we need to skip this RootDomain
    /// </summary>
    /// <param name="agentTrigger">Agent trigger configuration</param>
    /// <returns>If we need to skip this RootDomain</returns>
    public bool CanSkip(AgentTrigger agentTrigger)
    {
        if ((agentTrigger.RootdomainHasBounty ?? false) && !this.HasBounty)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(agentTrigger.RootdomainIncExcName) && !string.IsNullOrEmpty(agentTrigger.RootdomainName))
        {
            if (AgentTrigger.INCLUDE.Equals(agentTrigger.RootdomainIncExcName, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Name!, agentTrigger.RootdomainName);

                // if match success dont skip this rootdomain  
                return !match.Success;
            }
            else if (AgentTrigger.EXCLUDE.Equals(agentTrigger.RootdomainIncExcName, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Name!, agentTrigger.RootdomainName);

                // if match success skip this rootdomain 
                return match.Success;
            }
        }

        return false;
    }

    /// <summary>
    /// Add a new note
    /// </summary>
    /// <param name="agentName">The agent name</param>
    /// <param name="note">The new note</param>
    public void AddNewNote(string agentName, string note)
    {
        this.Notes.Add(new Note
        {
            CreatedBy = $"Agent {agentName}",
            Comment = note
        });
    }
}
