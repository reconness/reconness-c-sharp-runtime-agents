using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Infrastructure;

public class ProcessProviderFactory : IProcessProviderFactory
{
    public IProcessProvider Build()
    {
        return new ProcessProvider();
    }
}
