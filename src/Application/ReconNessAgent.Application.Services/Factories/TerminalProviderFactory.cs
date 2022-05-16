using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Infrastructure.Terminal;

namespace ReconNessAgent.Application.Services.Factories;

public class TerminalProviderFactory : ITerminalProviderFactory
{
    public ITerminalProvider CreateTerminalProvider(TerminalType type = TerminalType.BASH)
    {
        return type switch
        {
            TerminalType.BASH => new TerminalBashProvider(),
            _ => throw new ArgumentException(),
        };
    }
}
