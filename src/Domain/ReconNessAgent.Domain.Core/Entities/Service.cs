using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Service
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public int Port { get; set; }
        public Guid SubdomainId { get; set; }
        public string? Name { get; set; }

        public virtual Subdomain Subdomain { get; set; } = null!;
    }
}
