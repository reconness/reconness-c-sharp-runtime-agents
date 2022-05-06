namespace ReconNessAgent.Application.Providers;

public interface IProcessProvider
{
    void Start(string command);

    string? ReadLine();

    bool EndOfStream { get; }

    void Stop();
}
