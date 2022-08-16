using FluentAssertions;
using ReconNessAgent.Infrastructure.ScriptEngine;

namespace ReconNessAgent.Infrastructure.UnitTests;

public class CCharpScriptEngineProviderUnitTests
{
    [Fact]
    public async Task TestFierceOneParseInputAsyncMethod()
    {
        // Arrange

        var script = @"using ReconNessAgent.Domain.Core.ValueObjects;

                    var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""^.*?:\s(.*?opera.*?)\s\((.*?)\)"");
                    if (match.Success && match.Groups.Count == 3)
                    {
                        var subdomain = match.Groups[1].Value.EndsWith('.') ? match.Groups[1].Value.Substring(0, match.Groups[1].Value.Length - 1) : match.Groups[1].Value;
                        return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = subdomain };
                    }

                    return new TerminalOutputParse();";


        var scriptEngineService = new CCharpScriptEngineProvider(script);

        // Act
        var result = await scriptEngineService.ParseAsync("Found: pl.opera.com. (3.15.119.208)", 0);

        // Assert
        result.Should().NotBeNull();
        result.Subdomain.Should().Be("pl.opera.com");
        result.Ip.Should().Be("3.15.119.208");
    }

    [Fact]
    public async Task TestFierceTwoParseInputAsyncMethod()
    {
        // Arrange
        var script = @"using ReconNessAgent.Domain.Core.ValueObjects;

                    var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""^.*?:\s(.*?opera.*?)\s\((.*?)\)"");
                    if (match.Success && match.Groups.Count == 3)
                    {
                        var subdomain = match.Groups[1].Value.EndsWith('.') ? match.Groups[1].Value.Substring(0, match.Groups[1].Value.Length - 1) : match.Groups[1].Value;
                        return new TerminalOutputParse { Ip = match.Groups[2].Value, Subdomain = subdomain };
                    }

                    return new TerminalOutputParse();";


        var scriptEngineService = new CCharpScriptEngineProvider(script);

        // Act
        var result = await scriptEngineService.ParseAsync("SOA: nic1.opera.com. (185.26.183.160)", 0);

        // Assert
        result.Should().NotBeNull();
        result.Subdomain.Should().Be("nic1.opera.com");
        result.Ip.Should().Be("185.26.183.160");
    }

    [Fact]
    public async Task TestGoBusterOneParseInputAsyncMethod()
    {
        // Arrange
        var match = System.Text.RegularExpressions.Regex.Match("Found: acme5.opera.com", @"^Found:\s(.*opera.*)");
        if (match.Success)
        {
            var group = match.Groups;
            var ips = group[0].Value.Length;
        }

        var script = @"using ReconNessAgent.Domain.Core.ValueObjects;

                    if (lineInputCount < 13)
                    {
	                    return new TerminalOutputParse();
                    }

                    var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""^Found:\s(.*opera.*)"");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        return new TerminalOutputParse { Subdomain = match.Groups[1].Value };
                    }

                    return new TerminalOutputParse(); ";

        var scriptEngineService = new CCharpScriptEngineProvider(script);

        // Act
        var result = await scriptEngineService.ParseAsync("Found: acme5.opera.com", 20);

        // Assert
        result.Should().NotBeNull();
        result.Subdomain.Should().Be("acme5.opera.com");
    }

    [Fact]
    public async Task TestNmapParseInputAsyncMethod()
    {
        // Arrange
        var script = @"using ReconNessAgent.Domain.Core.ValueObjects;

                    var match = System.Text.RegularExpressions.Regex.Match(lineInput, @""(.*?)/tcp\s*open\s*(.*?)$"");
                    if (match.Success && match.Groups.Count == 3)
                    {
                        return new TerminalOutputParse { Service = match.Groups[2].Value, Port = int.Parse(match.Groups[1].Value) };
                    }

                    return new TerminalOutputParse();";

        var scriptEngineService = new CCharpScriptEngineProvider(script);

        // Act
        var result = await scriptEngineService.ParseAsync("22/tcp  open  ssh", 0);

        // Assert
        result.Should().NotBeNull();
        result.Service.Should().Be("ssh");
        result.Port.Should().Be(22);
    }
}
