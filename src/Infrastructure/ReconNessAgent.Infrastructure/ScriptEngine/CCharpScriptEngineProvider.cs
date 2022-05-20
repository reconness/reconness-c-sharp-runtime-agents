using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using Serilog;
using System.Reflection;

namespace ReconNessAgent.Infrastructure.ScriptEngine;

/// <summary>
/// This class implement the interface <see cref="IScriptEngineProvider"/> to parse the terminal output.
/// This particular implementation is using c# as script engine to parse the terminal output.
/// </summary>
public class CCharpScriptEngineProvider : IScriptEngineProvider
{
    private readonly string script;

    /// <summary>
    /// Initializes a new instance of the <see cref="CCharpScriptEngineProvider" /> class.
    /// </summary>
    /// <param name="script">The script that is going to parse the terminal output.</param>
    public CCharpScriptEngineProvider(string script)
    {
        this.script = script;
    }

    /// <inheritdoc/>
    public async Task<TerminalOutputParse> ParseAsync(string lineInput, int lineInputCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var globals = new Globals { lineInput = lineInput, lineInputCount = lineInputCount };
        return await CSharpScript.EvaluateAsync<TerminalOutputParse>(script,
            ScriptOptions.Default.WithImports("ReconNessAgent.Application.Models.ScriptParse")
            .AddReferences(
                Assembly.GetAssembly(typeof(TerminalOutputParse)),
                Assembly.GetAssembly(typeof(Exception)),
                Assembly.GetAssembly(typeof(System.Text.RegularExpressions.Regex)))
            , globals: globals, cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Global Class
/// </summary>
public class Globals
{
    public string? lineInput;

    public int lineInputCount;
}
