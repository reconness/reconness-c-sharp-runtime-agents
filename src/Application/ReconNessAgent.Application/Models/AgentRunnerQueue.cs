namespace ReconNessAgent.Application.Models;

/// <summary>
/// This class is what we received from the Pub/Sub provider to allow us run the command in a terminal and save the result from the terminal output
/// </summary>
public class AgentRunnerQueue
{
    /// <summary>
    /// We use the channel as identity to know what we are running. And with the format we can identify
    /// the agent, the target, the rootdomain and the subdomain
    /// 
    /// Ex. #20220319.1_nmap_yahoo_yahoo.com_www.yahoo.com
    /// 
    ///     agent: nmap
    ///     target: yahoo
    ///     rootdomain: yahoo.com
    ///     subdomain: www.yahoo.com
    ///     
    /// </summary>
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// A payload with the current concept (ex. target, rootdomain, subdomain) if the channel is generic
    /// Ex generic channel: #20220319.1_nmap_yahoo_yahoo.com_all
    /// 
    /// In this case the payload is a subdomain, and can be 
    /// Ex. www.yahoo.com
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// The command to run in the terminal
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// What position the command is in the order that we are running 
    /// Ex. We are running 10 commands for the same channel, and this is the number 6
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// The number for the server that we are using
    /// Ex. 3 if this is docker agent number 3
    /// </summary>
    public int ServerNumber { get; set; }
}
