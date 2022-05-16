using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ReconNessAgent.Application.Models;
using ReconNessAgent.Application.Providers;
using Serilog;
using System.Reflection;

namespace ReconNessAgent.Infrastructure.ScriptEngine;

public class CCharpScriptEngineProvider : IScriptEngineProvider
{
    private static readonly ILogger _logger = Log.ForContext<CCharpScriptEngineProvider>();

    private readonly string script;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="script">The script that is going to parse the console input.</param>
    public CCharpScriptEngineProvider(string script)
    {
        this.script = script;
    }

    /// <inheritdoc/>
    public async Task<ScriptParse> ParseAsync(string lineInput, int lineInputCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var globals = new Globals { lineInput = lineInput, lineInputCount = lineInputCount };
        return await CSharpScript.EvaluateAsync<ScriptParse>(script,
            ScriptOptions.Default.WithImports("ReconNessAgent.Application.Models.ScriptParse")
            .AddReferences(
                Assembly.GetAssembly(typeof(ScriptParse)),
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
    public string lineInput;

    public int lineInputCount;
}
