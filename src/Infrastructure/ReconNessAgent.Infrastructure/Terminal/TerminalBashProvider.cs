using ReconNessAgent.Application.Providers;
using Serilog;
using System.Diagnostics;

namespace ReconNessAgent.Infrastructure.Terminal;

/// <summary>
/// This class implement the interface <see cref="ITerminalProvider"/> to execute, read lines, check if the command finished and exit the terminal.
/// This particular implementation is using "/bin/bash" <see cref="Process"/> to run the command.
/// </summary>
public class TerminalBashProvider : ITerminalProvider
{
    private static readonly ILogger _logger = Log.ForContext<TerminalBashProvider>();

    private Process? process;

    /// <inheritdoc/>
    public void Execute(string command)
    {
        process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
    }

    /// <inheritdoc/>
    public async Task<string?> ReadLineAsync()
    {
        if (!Finished && process != null)
        {
            return await process.StandardOutput.ReadLineAsync();
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public bool Finished
    {
        get
        {
            if (process == null)
            {
                return true;
            }

            return process.StandardOutput.EndOfStream;
        }
    }

    /// <inheritdoc/>
    public void Exit()
    {
        if (process != null)
        {
            try
            {
                process.Kill(true);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
            }
            finally
            {
                process = null;
            }
        }
    }
}
