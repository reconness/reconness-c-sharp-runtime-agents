using ReconNessAgent.Application.Models.Enums;
using ReconNessAgent.Application.Providers;

namespace ReconNessAgent.Application.Factories;

/// <summary>
/// This interface expose a method to build the script engine <see cref="IScriptEngineProvider"/> based in the language that is going to parse the terminal output,
/// by default we use <see cref="ScriptEngineLanguage.C_CHARP"/>.
/// </summary>
public interface IScriptEngineProvideFactory
{
    /// <summary>
    /// Parse the terminal output and return what we need to save on database.
    /// </summary>
    /// <param name="script">Script to run by default is null.</param>
    /// <param name="language">The languge for the engine, by deafult <see cref="criptEngineLanguage.C_CHARP"/></param>
    /// <exception cref="ArgumentException">If the script language does not exist.</exception>
    IScriptEngineProvider CreateScriptEngineProvider(string script, ScriptEngineLanguage language = ScriptEngineLanguage.C_CHARP);
}
