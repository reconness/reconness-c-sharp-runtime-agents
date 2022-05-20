using ReconNessAgent.Application.Models;

namespace ReconNessAgent.Application.Providers;

/// <summary>
/// This interface expose the method to parse the terminal output.
/// </summary>
public interface IScriptEngineProvider
{
    /// <summary>
    /// Parse the terminal output and return what we need to save on database.
    /// </summary>
    /// <param name="lineOutput">The terminal output line.</param>
    /// <param name="lineOutputCount">the count of the terminal output line.</param>
    /// <param name="cancellationToken">Notification that operations should be canceled.</param>
    /// <returns>What we need to save on database.</returns>
    Task<TerminalOutputParse> ParseAsync(string lineOutput, int lineOutputCount, CancellationToken cancellationToken = default);
}

