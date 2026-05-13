using System.Collections.Generic;
using SourceToAI.CLI.App.Cli;

namespace SourceToAI.Tests.App;

public sealed class SourceToAiCliTests
{
    [Fact]
    public async Task Parse_PositionalTwoArgs_InvokesHandlerWithPaths()
    {
        string? gotExport = null;
        IReadOnlyList<string>? gotSolutions = null;
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _) =>
        {
            gotExport = export;
            gotSolutions = solutions;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["outDir", "solDir"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("outDir", gotExport);
        Assert.NotNull(gotSolutions);
        Assert.Equal(["solDir"], gotSolutions);
    }

    [Fact]
    public async Task Parse_MultiplePositionalArgs_InvokesHandlerWithAllPaths()
    {
        string? gotExport = null;
        IReadOnlyList<string>? gotSolutions = null;
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _) =>
        {
            gotExport = export;
            gotSolutions = solutions;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["out", "src1", "src2"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("out", gotExport);
        Assert.NotNull(gotSolutions);
        Assert.Equal(["src1", "src2"], gotSolutions);
    }

    [Fact]
    public async Task Parse_NamedExportAndInput_InvokesHandlerWithPaths()
    {
        string? gotExport = null;
        IReadOnlyList<string>? gotSolutions = null;
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _) =>
        {
            gotExport = export;
            gotSolutions = solutions;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["--export", "e", "--input", "s"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("e", gotExport);
        Assert.NotNull(gotSolutions);
        Assert.Equal(["s"], gotSolutions);
    }

    [Fact]
    public async Task Parse_MultipleNamedInputArgs_InvokesHandlerWithAllPaths()
    {
        string? gotExport = null;
        IReadOnlyList<string>? gotSolutions = null;
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _) =>
        {
            gotExport = export;
            gotSolutions = solutions;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["--export", "out", "--input", "src1", "--input", "src2"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("out", gotExport);
        Assert.NotNull(gotSolutions);
        Assert.Equal(["src1", "src2"], gotSolutions);
    }

    [Fact]
    public async Task Parse_NamedExportAndInputShortAlias_InvokesHandlerWithPaths()
    {
        string? gotExport = null;
        IReadOnlyList<string>? gotSolutions = null;
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _) =>
        {
            gotExport = export;
            gotSolutions = solutions;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["--export", "e", "-i", "s"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("e", gotExport);
        Assert.NotNull(gotSolutions);
        Assert.Equal(["s"], gotSolutions);
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
    public void Parse_OptionMissingValue_ProducesParseError()
    {
        var root = SourceToAiCli.CreateRootCommand((_, _, _) => Task.FromResult(0));
        var parseResult = root.Parse(["--export", "e", "--input"]);
        Assert.NotEmpty(parseResult.Errors);
    }
}
