using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Directory
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public string? Uri { get; set; }
        public string? StatusCode { get; set; }
        public string? Size { get; set; }
        public string? Method { get; set; }
        public Guid? SubdomainId { get; set; }

        public virtual Subdomain? Subdomain { get; set; }
    }
}
