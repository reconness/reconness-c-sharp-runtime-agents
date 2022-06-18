using System.Text.RegularExpressions;

namespace ReconNessAgent.Domain.Core.Entities;

public partial class Subdomain : BaseEntity
{
    public Subdomain()
    {
        Directories = new HashSet<Directory>();
        EventTracks = new HashSet<EventTrack>();
        Notes = new HashSet<Note>();
        Services = new HashSet<Service>();
        Labels = new HashSet<Label>();
    }

    public Guid Id { get; set; }
    public bool? HasHttpOpen { get; set; }
    public bool? IsMainPortal { get; set; }
    public string? Name { get; set; }
    public bool? IsAlive { get; set; }
    public string? IpAddress { get; set; }
    public bool? Takeover { get; set; }
    public Guid RootDomainId { get; set; }
    public string? Technology { get; set; }
    public bool? HasBounty { get; set; }
    public string? AgentsRanBefore { get; set; }

    public virtual RootDomain RootDomain { get; set; } = null!;
    public virtual ICollection<Directory> Directories { get; set; }
    public virtual ICollection<EventTrack> EventTracks { get; set; }
    public virtual ICollection<Note> Notes { get; set; }
    public virtual ICollection<Service> Services { get; set; }

    public virtual ICollection<Label> Labels { get; set; }

    /// <summary>
    /// If we need to skip this Subdomain
    /// </summary>
    /// <param name="agentTrigger">Agent trigger configuration</param>
    /// <returns>If we need to skip this Subdomain</returns>
    public bool CanSkip(AgentTrigger agentTrigger)
    {
        if (agentTrigger.SubdomainHasBounty && (this.HasBounty == null || !this.HasBounty.Value))
        {
            return true;
        }

        if (agentTrigger.SubdomainHasHttpOrHttpsOpen && (this.HasHttpOpen == null || !this.HasHttpOpen.Value))
        {
            return true;
        }

        if (agentTrigger.SubdomainIsAlive && (this.IsAlive == null || !this.IsAlive.Value))
        {
            return true;
        }

        if (agentTrigger.SubdomainIsMainPortal && (this.IsMainPortal == null || !this.IsMainPortal.Value))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(agentTrigger.SubdomainIncExcName) && !string.IsNullOrEmpty(agentTrigger.SubdomainName))
        {
            if (AgentTrigger.INCLUDE.Equals(agentTrigger.SubdomainIncExcName, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Name!, agentTrigger.SubdomainName);

                // if match success dont skip this subdomain  
                return !match.Success;
            }
            else if (AgentTrigger.EXCLUDE.Equals(agentTrigger.SubdomainIncExcName, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Name!, agentTrigger.SubdomainName);

                // if match success skip this subdomain 
                return match.Success;
            }
        }

        if (!string.IsNullOrEmpty(agentTrigger.SubdomainIncExcServicePort) && !string.IsNullOrEmpty(agentTrigger.SubdomainServicePort))
        {
            if (AgentTrigger.INCLUDE.Equals(agentTrigger.SubdomainIncExcServicePort, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var service in this.Services)
                {
                    var matchService = Regex.Match(service.Name!, agentTrigger.SubdomainServicePort);
                    var matchPort = Regex.Match(service.Port.ToString(), agentTrigger.SubdomainServicePort);

                    // if match success service or port dont skip this subdomain  
                    if (matchService.Success || matchPort.Success)
                    {
                        return false;
                    }
                }

                return true;
            }
            else if (AgentTrigger.EXCLUDE.Equals(agentTrigger.SubdomainIncExcServicePort, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var service in this.Services)
                {
                    var matchService = Regex.Match(service.Name!, agentTrigger.SubdomainServicePort);
                    var matchPort = Regex.Match(service.Port.ToString(), agentTrigger.SubdomainServicePort);

                    // if match success services or port skip this subdomain  
                    if (matchService.Success || matchPort.Success)
                    {
                        return true;
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(agentTrigger.SubdomainIncExcIp) && !string.IsNullOrEmpty(agentTrigger.SubdomainIp))
        {
            if (AgentTrigger.INCLUDE.Equals(agentTrigger.SubdomainIncExcIp, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.IpAddress!, agentTrigger.SubdomainIp);

                // if match success dont skip this subdomain  
                return !match.Success;
            }
            else if (AgentTrigger.EXCLUDE.Equals(agentTrigger.SubdomainIncExcIp, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.IpAddress!, agentTrigger.SubdomainIp);

                // if match success skip this subdomain 
                return match.Success;
            }
        }

        if (!string.IsNullOrEmpty(agentTrigger.SubdomainIncExcTechnology) && !string.IsNullOrEmpty(agentTrigger.SubdomainTechnology))
        {
            if (AgentTrigger.INCLUDE.Equals(agentTrigger.SubdomainIncExcTechnology, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Technology!, agentTrigger.SubdomainTechnology);

                // if match success dont skip this subdomain  
                return !match.Success;
            }
            else if (AgentTrigger.EXCLUDE.Equals(agentTrigger.SubdomainIncExcTechnology, StringComparison.OrdinalIgnoreCase))
            {
                var match = Regex.Match(this.Technology!, agentTrigger.SubdomainTechnology);

                // if match success skip this subdomain 
                return match.Success;
            }
        }

        if (!string.IsNullOrEmpty(agentTrigger.SubdomainIncExcLabel) && !string.IsNullOrEmpty(agentTrigger.SubdomainLabel))
        {
            if (AgentTrigger.INCLUDE.Equals(agentTrigger.SubdomainIncExcLabel, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var label in this.Labels)
                {
                    var match = Regex.Match(label.Name!, agentTrigger.SubdomainLabel);

                    // if match success label dont skip this subdomain  
                    if (match.Success)
                    {
                        return false;
                    }
                }

                return true;
            }
            else if (AgentTrigger.EXCLUDE.Equals(agentTrigger.SubdomainIncExcLabel, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var label in this.Labels)
                {
                    var match = Regex.Match(label.Name!, agentTrigger.SubdomainLabel);

                    // if match success label skip this subdomain  
                    if (match.Success)
                    {
                        return true;
                    }
                };
            }
        }

        return false;
    }
}
