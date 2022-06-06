using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Target
    {
        public Target()
        {
            EventTracks = new HashSet<EventTrack>();
            Notes = new HashSet<Note>();
            RootDomains = new HashSet<RootDomain>();
        }

        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
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
    }
}
