using ReconNessAgent.Application.Factories;
using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;
using ReconNessAgent.Infrastructure.ScriptEngine;

namespace ReconNessAgent.Application.Services.Factories;

public class ScriptEngineProvideFactory : IScriptEngineProvideFactory
{
    public IScriptEngineProvider CreateScriptEngineProvider(string script, ScriptEngineLanguage language = ScriptEngineLanguage.C_CHARP)
    {
        return language switch
        {
            ScriptEngineLanguage.C_CHARP => new CCharpScriptEngineProvider(script),
            _ => throw new ArgumentException(),
        };
    }
}
