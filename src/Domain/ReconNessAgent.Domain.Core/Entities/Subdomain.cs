using System.Net;
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

    public string? ExtraFields { get; set; }
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
        if (agentTrigger.SubdomainHasBounty != (this.HasBounty ?? false))
        {
            return true;
        }

        if (agentTrigger.SubdomainHasHttpOrHttpsOpen ?? false && (this.HasHttpOpen == null || !this.HasHttpOpen.Value))
        {
            return true;
        }

        if (agentTrigger.SubdomainIsAlive ?? false && (this.IsAlive == null || !this.IsAlive.Value))
        {
            return true;
        }

        if (agentTrigger.SubdomainIsMainPortal ?? false && (this.IsMainPortal == null || !this.IsMainPortal.Value))
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
                    var matchPort = Regex.Match(service.Port.ToString() ?? String.Empty, agentTrigger.SubdomainServicePort);

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
                    var matchPort = Regex.Match(service.Port.ToString() ?? String.Empty, agentTrigger.SubdomainServicePort);

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

    /// <summary>
    /// Assign Ip address to the subdomain
    /// </summary>
    /// <param name="ipAddress">The IP address</param>
    public void UpdateSubdomainIpAddress(string ipAddress)
    {
        static bool ValidateIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString) || ipString.Count(c => c == '.') != 3)
            {
                return false;
            }

            return IPAddress.TryParse(ipString, out IPAddress? address);
        }

        if (ValidateIPv4(ipAddress) && this.IpAddress != ipAddress)
        {
            this.IpAddress = ipAddress;
        }
    }

    /// <summary>
    /// Update the subdomain if is Alive
    /// </summary>
    /// <param name="isAlive">If is alive</param>
    public void UpdateSubdomainIsAlive(bool isAlive)
    {
        if (this.IsAlive != isAlive)
        {
            this.IsAlive = isAlive;
        }
    }

    /// <summary>
    /// Update the subdomain if it has http port open
    /// </summary>
    /// <param name="hasHttpOpen">If has Http port open</param>
    public void UpdateSubdomainHasHttpOpen(bool hasHttpOpen)
    {
        if (this.HasHttpOpen != hasHttpOpen)
        {
            this.HasHttpOpen = hasHttpOpen;
        }
    }

    /// <summary>
    /// Update the subdomain if it can be takeover
    /// </summary>
    /// <param name="takeover">If has takeover</param>
    public void UpdateSubdomainTakeover(bool takeover)
    {
        if (this.Takeover != takeover)
        {
            this.Takeover = takeover;
        }
    }

    /// <summary>
    /// Update the subdomain with directory discovery
    /// </summary>
    /// <param name="httpDirectory">The http directory</param>
    /// <param name="statusCode">the status code</param>
    /// <param name="method">The method</param>
    /// <param name="size">The size</param>
    public void UpdateSubdomainDirectory(string httpDirectory, string? statusCode, string? method, string? size)
    {
        httpDirectory = httpDirectory.TrimEnd('/').TrimEnd();
        if (this.Directories == null)
        {
            this.Directories = new List<Domain.Core.Entities.Directory>();
        }

        if (this.Directories.Any(d => d.Uri == httpDirectory))
        {
            return;
        }

        var directory = new Domain.Core.Entities.Directory()
        {
            Uri = httpDirectory,
            StatusCode = statusCode,
            Method = method,
            Size = size
        };

        this.Directories.Add(directory);
    }

    /// <summary>
    /// Update the subdomain if is a new service with open port
    /// </summary>
    /// <param name="service">The service running in that subdomain</param>
    /// <param name="port">The port</param>
    public void UpdateSubdomainService(string service, int? port)
    {
        if (this.Services == null)
        {
            this.Services = new List<Service>();
        }

        var newService = new Service
        {
            Name = service.ToLower(),
            Port = port
        };

        if (!this.Services.Any(s => s.Name == newService.Name && s.Port == newService.Port))
        {
            this.Services.Add(newService);
        }
    }

    /// <summary>
    /// Update the subdomain Technology
    /// </summary>
    /// <param name="technology">The technology running in the subdomain</param>
    public void UpdateSubdomainTechnology(string technology)
    {
        if (!technology.Equals(this.Technology, StringComparison.OrdinalIgnoreCase))
        {
            this.Technology = technology;
        }
    }

    /// <summary>
    /// Update the subdomain ExtraFields
    /// </summary>
    /// <param name="extraFields">The ExtraFields</param>
    public void UpdateSubdomainExtraFields(string extraFields)
    {
        this.ExtraFields += extraFields;
    }

    /// <summary>
    /// Update the subdomain label
    /// </summary>
    /// <param name="label">The label</param>
    public void UpdateSubdomainLabel(string label)
    {
        if (!this.Labels.Any(l => label.Equals(l.Name, StringComparison.OrdinalIgnoreCase)))
        {
            var random = new Random();
            var newLabel = new Label
            {
                Name = label,
                Color = string.Format("#{0:X6}", random.Next(0x1000000))
            };

            this.Labels.Add(newLabel);
        }
    }

    /// <summary>
    /// Add a new note
    /// </summary>
    /// <param name="agentName">The agent name</param>
    /// <param name="note">The new note</param>
    public void AddNewNote(string agentName, string note)
    {
        this.Notes.Add(new Note
        {
            CreatedBy = $"Agent {agentName}",
            Comment = note
        });
    }
}
