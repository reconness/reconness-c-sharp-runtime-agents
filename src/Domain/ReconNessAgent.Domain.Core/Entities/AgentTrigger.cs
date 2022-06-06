﻿using System;
using System.Collections.Generic;

namespace ReconNessAgent.Domain.Core.Entities
{
    public partial class AgentTrigger
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Deleted { get; set; }
        public Guid AgentId { get; set; }
        public bool RootdomainHasBounty { get; set; }
        public string? RootdomainIncExcName { get; set; }
        public string? RootdomainName { get; set; }
        public bool SkipIfRunBefore { get; set; }
        public bool SubdomainHasHttpOrHttpsOpen { get; set; }
        public string? SubdomainIp { get; set; }
        public string? SubdomainIncExcIp { get; set; }
        public string? SubdomainIncExcLabel { get; set; }
        public string? SubdomainIncExcName { get; set; }
        public string? SubdomainIncExcServicePort { get; set; }
        public string? SubdomainIncExcTechnology { get; set; }
        public bool SubdomainIsAlive { get; set; }
        public bool SubdomainIsMainPortal { get; set; }
        public string? SubdomainLabel { get; set; }
        public string? SubdomainName { get; set; }
        public string? SubdomainServicePort { get; set; }
        public string? SubdomainTechnology { get; set; }
        public bool TargetHasBounty { get; set; }
        public string? TargetIncExcName { get; set; }
        public string? TargetName { get; set; }
        public bool SubdomainHasBounty { get; set; }

        public virtual Agent Agent { get; set; } = null!;
    }
}
