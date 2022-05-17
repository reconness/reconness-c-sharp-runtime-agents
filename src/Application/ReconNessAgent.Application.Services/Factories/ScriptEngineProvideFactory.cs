using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Infrastructure.ScriptEngine;

namespace ReconNessAgent.Application.Services.Factories;

/// <summary>
/// This class implement the interface <see cref="IScriptEngineProvideFactory"/> to build the script engine 
/// <see cref="IScriptEngineProvider"/> based in the language that is going to parse the terminal output,
/// by default we use <see cref="ScriptEngineLanguage.C_CHARP"/>.
/// </summary>
public class ScriptEngineProvideFactory : IScriptEngineProvideFactory
{
    /// <inheritdoc/>
    public IScriptEngineProvider CreateScriptEngineProvider(string script, ScriptEngineLanguage language = ScriptEngineLanguage.C_CHARP)
    {
        return language switch
        {
            ScriptEngineLanguage.C_CHARP => new CCharpScriptEngineProvider(script),
            _ => throw new ArgumentException(),
        };
    }
}
