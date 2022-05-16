using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Application.Factories;

public interface IScriptEngineProvideFactory
{
    /// <summary>
    /// Parse the terminal input and return what we need to save on database
    /// </summary>
    /// <param name="script">Script to run by default is null</param>
    /// <param name="language">The languge for the engine, by deafult c#</param>
    IScriptEngineProvider CreateScriptEngineProvider(string script, ScriptEngineLanguage language = ScriptEngineLanguage.C_CHARP);
}
