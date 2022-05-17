using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Infrastructure.Terminal;

namespace ReconNessAgent.Application.Services.Factories;

/// <summary>
/// This class implement the interface <see cref="ITerminalProviderFactory"/> that is going to build the <see cref="ITerminalProvider"/>
/// to run the command based in the type <see cref="TerminalType" />, by default we used <see cref="TerminalType.BASH" />.
/// </summary>
public class TerminalProviderFactory : ITerminalProviderFactory
{
    /// <inheritdoc/>
    public ITerminalProvider CreateTerminalProvider(TerminalType type = TerminalType.BASH)
    {
        return type switch
        {
            TerminalType.BASH => new TerminalBashProvider(),
            _ => throw new ArgumentException(),
        };
    }
}
