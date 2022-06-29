using System.Text.RegularExpressions;

namespace ReconNessAgent.Domain.Core.Entities;

public partial class Target : BaseEntity
{
    public Target()
    {
        EventTracks = new HashSet<EventTrack>();
        Notes = new HashSet<Note>();
        RootDomains = new HashSet<RootDomain>();
    }

    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? InScope { get; set; }
    public bool IsPrivate { get; set; }
    public string? OutOfScope { get; set; }
    public string? BugBountyProgramUrl { get; set; }
    public bool HasBounty { get; set; }
    public string? AgentsRanBefore { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }

    public virtual ICollection<EventTrack> EventTracks { get; set; }
    public virtual ICollection<Note> Notes { get; set; }
    public virtual ICollection<RootDomain> RootDomains { get; set; }

    /// <summary>
    /// If we need to skip this Target
    /// </summary>
    /// <param name="target">The Target</param>
    /// <param name="agentTrigger">Agent trigger configuration</param>
    /// <returns>If we need to skip this Target</returns>
    public bool CanSkip(AgentTrigger agentTrigger)
    {
        if (agentTrigger.TargetHasBounty && !this.HasBounty)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(agentTrigger.TargetIncExcName) && !string.IsNullOrEmpty(agentTrigger.TargetName))
        {
            if (AgentTrigger.INCLUDE.Equals(agentTrigger.TargetIncExcName, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Name!, agentTrigger.TargetName);

                // if match success dont skip this target  
                return !match.Success;
            }
            else if (AgentTrigger.EXCLUDE.Equals(agentTrigger.TargetIncExcName, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Name!, agentTrigger.TargetName);

                // if match success skip this target 
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
