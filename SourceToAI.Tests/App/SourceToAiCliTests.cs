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
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _, _, _) =>
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
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _, _, _) =>
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
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _, _, _) =>
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
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _, _, _) =>
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
    public async Task Parse_MultipleExcludeArgs_PassedToHandler()
    {
        IReadOnlyList<string>? gotExcludes = null;
        var root = SourceToAiCli.CreateRootCommand((_, _, _, excludes, _) =>
        {
            gotExcludes = excludes;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse([
            "--export", "out",
            "--input", "src",
            "--exclude", "wwwroot/lib/**",
            "--exclude", "**/vis-timeline-graph2d.min.js",
        ]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.NotNull(gotExcludes);
        Assert.Equal(
            new[] { "wwwroot/lib/**", "**/vis-timeline-graph2d.min.js" },
            gotExcludes);
    }

    [Fact]
    public async Task Parse_NamedExportAndInputShortAlias_InvokesHandlerWithPaths()
    {
        string? gotExport = null;
        IReadOnlyList<string>? gotSolutions = null;
        var root = SourceToAiCli.CreateRootCommand((export, solutions, _, _, _) =>
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
        var root = SourceToAiCli.CreateRootCommand((_, _, _, _, _) => Task.FromResult(0));
        var parseResult = root.Parse(["a", "b", "--export", "e", "--input", "i"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Parse_NamedExportAndGacOnly_InvokesHandlerWithGacPatterns()
    {
        string? gotExport = null;
        IReadOnlyList<string>? gotGac = null;
        var root = SourceToAiCli.CreateRootCommand((export, _, gac, _, _) =>
        {
            gotExport = export;
            gotGac = gac;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["--export", "out", "--gac", "Contoso.*.dll"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.Equal("out", gotExport);
        Assert.NotNull(gotGac);
        Assert.Equal(["Contoso.*.dll"], gotGac);
    }

    [Fact]
    public async Task Parse_PositionalExportAndGac_InvokesHandlerWithoutSolutionPaths()
    {
        IReadOnlyList<string>? gotSolutions = null;
        IReadOnlyList<string>? gotGac = null;
        var root = SourceToAiCli.CreateRootCommand((_, solutions, gac, _, _) =>
        {
            gotSolutions = solutions;
            gotGac = gac;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["out", "--gac", "Acme.*.dll"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.NotNull(gotSolutions);
        Assert.Empty(gotSolutions);
        Assert.NotNull(gotGac);
        Assert.Equal(["Acme.*.dll"], gotGac);
    }

    [Fact]
    public async Task Parse_GacAndInputCombined_InvokesHandlerWithBoth()
    {
        IReadOnlyList<string>? gotSolutions = null;
        IReadOnlyList<string>? gotGac = null;
        var root = SourceToAiCli.CreateRootCommand((_, solutions, gac, _, _) =>
        {
            gotSolutions = solutions;
            gotGac = gac;
            return Task.FromResult(0);
        });

        var parseResult = root.Parse(["--export", "e", "--input", "src", "--gac", "Contoso.*.dll"]);
        Assert.Empty(parseResult.Errors);

        var exitCode = await parseResult.InvokeAsync(
            parseResult.InvocationConfiguration,
            TestContext.Current.CancellationToken);
        Assert.Equal(0, exitCode);
        Assert.NotNull(gotSolutions);
        Assert.Equal(["src"], gotSolutions);
        Assert.NotNull(gotGac);
        Assert.Equal(["Contoso.*.dll"], gotGac);
    }

    [Fact]
    public void Parse_OptionMissingValue_ProducesParseError()
    {
        var root = SourceToAiCli.CreateRootCommand((_, _, _, _, _) => Task.FromResult(0));
        var parseResult = root.Parse(["--export", "e", "--input"]);
        Assert.NotEmpty(parseResult.Errors);
    }
}
