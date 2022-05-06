namespace ReconNessAgent.Application.Providers;

public interface IProcessProvider
{
    void Start(string command);

    Task<string?> ReadLineAsync();

    bool EndOfStream { get; }

    void Stop();
}
