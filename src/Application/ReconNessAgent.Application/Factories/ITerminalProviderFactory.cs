using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Application.Factories;

/// <summary>
/// This interface expose a methof to build the <see cref="ITerminalProvider"/> to run the command based in the type 
/// <see cref="TerminalType" />, by default we used <see cref="TerminalType.BASH" />.
/// </summary>
public interface ITerminalProviderFactory
{
    /// <summary>
    /// This method build the <see cref="ITerminalProvider"/> to run the command based in the type 
    /// <see cref="TerminalType" />, by default we used <see cref="TerminalType.BASH" />.
    /// </summary>
    /// <param name="type">The terminal type, by default we used <see cref="TerminalType.BASH"/></param>
    /// <returns>A <see cref="ITerminalProvider"/></returns>
    /// <exception cref="ArgumentException">If the terminal type does not exist.</exception>
    ITerminalProvider CreateTerminalProvider(TerminalType type = TerminalType.BASH);
}
