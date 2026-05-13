using SourceToAI.CLI.App.Cli;

namespace SourceToAI.Tests.App;

public sealed class SourceToAiCliTests
{
    [Fact]
    public async Task Parse_PositionalTwoArgs_InvokesHandlerWithPaths()
    {
        string? gotExport = null;
        string? gotSolution = null;
        var root = SourceToAiCli.CreateRootCommand((export, solution, _) =>
        {
            gotExport = export;
            gotSolution = solution;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["outDir", "solDir"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("outDir", gotExport);
        Assert.Equal("solDir", gotSolution);
    }

    [Fact]
    public async Task Parse_NamedExportAndInput_InvokesHandlerWithPaths()
    {
        string? gotExport = null;
        string? gotSolution = null;
        var root = SourceToAiCli.CreateRootCommand((export, solution, _) =>
        {
            gotExport = export;
            gotSolution = solution;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["--export", "e", "--input", "s"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("e", gotExport);
        Assert.Equal("s", gotSolution);
    }

    [Fact]
    public async Task Parse_NamedExportAndInputShortAlias_InvokesHandlerWithPaths()
    {
        string? gotExport = null;
        string? gotSolution = null;
        var root = SourceToAiCli.CreateRootCommand((export, solution, _) =>
        {
            gotExport = export;
            gotSolution = solution;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["--export", "e", "-i", "s"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("e", gotExport);
        Assert.Equal("s", gotSolution);
    }

    [Fact]
    public async Task Parse_PositionalAndNamed_ReturnsErrorExitCode()
    {
        var root = SourceToAiCli.CreateRootCommand((_, _, _) => Task.FromResult(0));
        var parseResult = root.Parse(["a", "b", "--export", "e", "--input", "i"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Parse_ExtraToken_ProducesParseError()
    {
        var root = SourceToAiCli.CreateRootCommand((_, _, _) => Task.FromResult(0));
        var parseResult = root.Parse(["a", "b", "c"]);
        Assert.NotEmpty(parseResult.Errors);
    }
}
