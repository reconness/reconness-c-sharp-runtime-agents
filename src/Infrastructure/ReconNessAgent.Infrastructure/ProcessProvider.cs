using ReconNessAgent.Application.Providers;
using Serilog;
using System.Diagnostics;

namespace ReconNessAgent.Infrastructure;

public class ProcessProvider : IProcessProvider
{
    private static readonly ILogger _logger = Log.ForContext<ProcessProvider>();

    private Process process;

    public void Start(string command)
    {
        this.process = new Process()
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

    public string? ReadLine()
    {
        if (!this.EndOfStream)
        {
            return this.process.StandardOutput.ReadLine();
        }

        return string.Empty;
    }

    public bool EndOfStream
    {
        get
        {
            if (this.process == null)
            {
                return true;
            }

            return this.process.StandardOutput.EndOfStream;
        }
    }

    public void Stop()
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
