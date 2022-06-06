using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class Reference
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public string? Url { get; set; }
        public string? Categories { get; set; }
    }
}
