using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Note
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public string? CreatedBy { get; set; }
        public Guid? SubdomainId { get; set; }
        public Guid? RootDomainId { get; set; }
        public string? Comment { get; set; }
        public Guid? TargetId { get; set; }

        public virtual RootDomain? RootDomain { get; set; }
        public virtual Subdomain? Subdomain { get; set; }
        public virtual Target? Target { get; set; }
    }
}
