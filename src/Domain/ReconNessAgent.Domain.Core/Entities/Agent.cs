using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Agent
    {
        public Agent()
        {
            AgentRunners = new HashSet<AgentRunner>();
            EventTracks = new HashSet<EventTrack>();
            Categories = new HashSet<Category>();
        }

        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public string? Name { get; set; }
        public DateTime? LastRun { get; set; }
        public string? Command { get; set; }
        public string? Script { get; set; }
        public string? Repository { get; set; }
        public string? AgentType { get; set; }
        public string? ConfigurationFileName { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? CreatedBy { get; set; }
        public string? Target { get; set; }
        public string? Image { get; set; }

        public virtual AgentTrigger AgentTrigger { get; set; } = null!;
        public virtual ICollection<AgentRunner> AgentRunners { get; set; }
        public virtual ICollection<EventTrack> EventTracks { get; set; }

        public virtual ICollection<Category> Categories { get; set; }
    }
}
