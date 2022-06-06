using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Label
    {
        public Label()
        {
            Subdomains = new HashSet<Subdomain>();
        }

        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public string? Name { get; set; }
        public string? Color { get; set; }

        public virtual ICollection<Subdomain> Subdomains { get; set; }
    }
}
