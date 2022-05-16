namespace ReconNessAgent.Application.Providers;

public interface ITerminalProvider
{
    void Execute(string command);

    Task<string?> ReadLineAsync();

    bool Finished { get; }

    void Exit();
}
