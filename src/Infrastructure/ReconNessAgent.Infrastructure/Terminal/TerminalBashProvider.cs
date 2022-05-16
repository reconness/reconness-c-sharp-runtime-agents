using ReconNessAgent.Application.Providers;
using Serilog;
using System.Diagnostics;

namespace ReconNessAgent.Infrastructure.Terminal;

public class TerminalBashProvider : ITerminalProvider
{
    private static readonly ILogger _logger = Log.ForContext<TerminalBashProvider>();

    private Process process;

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

    public async Task<string?> ReadLineAsync()
    {
        if (!Finished)
        {
            return await process.StandardOutput.ReadLineAsync();
        }

        return string.Empty;
    }

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
