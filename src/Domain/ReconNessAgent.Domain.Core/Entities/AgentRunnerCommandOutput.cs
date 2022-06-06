using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class AgentRunnerCommandOutput
    {
        public Guid Id { get; set; }
        public string? Output { get; set; }
        public Guid AgentRunnerCommandId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }

        public virtual AgentRunnerCommand AgentRunnerCommand { get; set; } = null!;
    }
}
