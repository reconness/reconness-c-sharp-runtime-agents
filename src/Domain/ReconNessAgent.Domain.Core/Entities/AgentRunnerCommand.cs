using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class AgentRunnerCommand
    {
        public AgentRunnerCommand()
        {
            AgentRunnerCommandOutputs = new HashSet<AgentRunnerCommandOutput>();
        }

        public Guid Id { get; set; }
        public int Status { get; set; }
        public string? Command { get; set; }
        public int Number { get; set; }
        public int Server { get; set; }
        public Guid AgentRunnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }

        public virtual AgentRunner AgentRunner { get; set; } = null!;
        public virtual ICollection<AgentRunnerCommandOutput> AgentRunnerCommandOutputs { get; set; }
    }
}
