using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class AgentsSetting
    {
        public Guid Id { get; set; }
        public int Strategy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public int AgentServerCount { get; set; }
    }
}
