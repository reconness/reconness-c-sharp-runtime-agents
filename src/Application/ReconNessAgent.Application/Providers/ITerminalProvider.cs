namespace ReconNessAgent.Application.Providers;

/// <summary>
/// This interface expose methods to execute, read lines, check if the command finished and exit the terminal.
/// </summary>
public interface ITerminalProvider
{
    /// <summary>
    /// Executa a command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    void Execute(string command);

    /// <summary>
    /// Obtain the next line read from the terminal process.
    /// </summary>
    /// <returns>The next line read from the terminal.</returns>
    Task<string?> ReadLineAsync();

    /// <summary>
    /// If the command finished.
    /// </summary>
    bool Finished { get; }

    /// <summary>
    /// Exit the terminal.
    /// </summary>
    void Exit();
}
