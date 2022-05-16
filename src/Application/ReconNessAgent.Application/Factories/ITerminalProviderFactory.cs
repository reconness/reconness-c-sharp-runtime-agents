using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Application.Factories;

public interface ITerminalProviderFactory
{
    ITerminalProvider CreateTerminalProvider(TerminalType type = TerminalType.BASH);
}
