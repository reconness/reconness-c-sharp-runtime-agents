using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Category
    {
        public Category()
        {
            Agents = new HashSet<Agent>();
        }

        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public string? Name { get; set; }

        public virtual ICollection<Agent> Agents { get; set; }
    }
}
