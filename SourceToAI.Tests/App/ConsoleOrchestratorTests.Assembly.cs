using Microsoft.Extensions.DependencyInjection;
using Moq;
using SourceToAI.CLI.App;
using SourceToAI.CLI.Configuration;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Decompilation;
using SourceToAI.CLI.Services.Discovery;
using SourceToAI.CLI.Services.Export;
using SourceToAI.CLI.Services.Integration;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;
using System.IO;

namespace SourceToAI.Tests.App;

public partial class ConsoleOrchestratorTests
{
    [Fact]
    public async Task RunAsync_with_assembly_input_calls_decompiler_then_discovery_on_decompiled_root()
    {
        using var export = new TempWorkspace();
        using var assemblyWs = new TempWorkspace();
        using var multiViewSp = CreateMultiViewServiceProvider();

        var dllPath = Path.Combine(assemblyWs.Root, "AsmOrch.dll");
        await File.WriteAllTextAsync(dllPath, string.Empty, TestContext.Current.CancellationToken);

        var decompiledProjectDir = Path.Combine(assemblyWs.Root, "decompiled");
        Directory.CreateDirectory(decompiledProjectDir);
        var csprojPath = Path.Combine(decompiledProjectDir, "AsmOrch.csproj");
        await File.WriteAllTextAsync(
            csprojPath,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);
        var csPath = Path.Combine(decompiledProjectDir, "Class1.cs");
        await File.WriteAllTextAsync(csPath, "namespace Asm; public class Class1 { }", TestContext.Current.CancellationToken);

        var project = new ProjectDefinition("AsmOrch", csprojPath);
        var plannedExportRoot = Path.GetFullPath(Path.Combine(export.Root, "Isolated", "AsmOrch"));
        var expectedDecompileDir = Path.GetFullPath(Path.Combine(plannedExportRoot, "decompile"));
        var effectiveRoot = Path.GetFullPath(decompiledProjectDir);

        var sequence = new MockSequence();
        var assemblyDecompiler = new Mock<IAssemblyDecompilerService>(MockBehavior.Strict);
        assemblyDecompiler.InSequence(sequence)
            .Setup(d => d.DecompileToProjectDirectory(
                Path.GetFullPath(dllPath),
                expectedDecompileDir,
                It.IsAny<CancellationToken>()))
            .Returns(effectiveRoot);

        var solutionDiscovery = new Mock<ISolutionDiscoveryService>(MockBehavior.Strict);
        solutionDiscovery.InSequence(sequence)
            .Setup(s => s.GetSolutionName(effectiveRoot))
            .Returns(ExtractionResult<string>.Success("AsmOrch"));
        solutionDiscovery.InSequence(sequence)
            .Setup(s => s.FindProjects(effectiveRoot))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([project]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery
            .Setup(f => f.FindSolutionDocs(effectiveRoot, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project, It.IsAny<string>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([csPath]));
        fileDiscovery
            .Setup(f => f.FindUnmappedDirectories(effectiveRoot, It.IsAny<IReadOnlyList<ProjectDefinition>>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<(string, List<string>)>>.Success([]));

        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            assemblyDecompiler.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiViewSp.GetRequiredService<IMultiViewExportService>(),
            multiViewSp.GetRequiredService<IMultiViewReadmeMarkdownGenerator>(),
            TestAppSettingsFactory.Default(),
            [post.Object]);

        var markerPath = Path.Combine(export.Root, MultiViewExportPaths.SafetyMarkerFileName);
        await File.WriteAllTextAsync(markerPath, "previous run", TestContext.Current.CancellationToken);

        Assert.True(await sut.RunAsync([dllPath], export.Root));

        assemblyDecompiler.Verify(
            d => d.DecompileToProjectDirectory(
                Path.GetFullPath(dllPath),
                expectedDecompileDir,
                It.IsAny<CancellationToken>()),
            Times.Once);
        solutionDiscovery.Verify(s => s.GetSolutionName(effectiveRoot), Times.Once);
        solutionDiscovery.Verify(s => s.FindProjects(effectiveRoot), Times.Once);

        post.Verify(p => p.ExecuteAsync("AsmOrch", plannedExportRoot), Times.Once);
    }

    [Fact]
    public async Task RunAsync_skips_failed_assembly_and_processes_next_assembly_source()
    {
        using var export = new TempWorkspace();
        using var assemblyWs = new TempWorkspace();
        using var multiViewSp = CreateMultiViewServiceProvider();

        var dllFail = Path.Combine(assemblyWs.Root, "AsmFail.dll");
        var dllOk = Path.Combine(assemblyWs.Root, "AsmOk.dll");
        await File.WriteAllTextAsync(dllFail, string.Empty, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(dllOk, string.Empty, TestContext.Current.CancellationToken);

        var decompiledOkDir = Path.Combine(assemblyWs.Root, "decompiled_ok");
        Directory.CreateDirectory(decompiledOkDir);
        var csprojOk = Path.Combine(decompiledOkDir, "AsmOk.csproj");
        await File.WriteAllTextAsync(
            csprojOk,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);
        var csOk = Path.Combine(decompiledOkDir, "Class1.cs");
        await File.WriteAllTextAsync(csOk, "namespace Asm; public class Class1 { }", TestContext.Current.CancellationToken);

        var projectOk = new ProjectDefinition("AsmOk", csprojOk);
        var plannedFailDecompile = Path.GetFullPath(Path.Combine(export.Root, "Isolated", "AsmFail", "decompile"));
        var plannedOkRoot = Path.GetFullPath(Path.Combine(export.Root, "Isolated", "AsmOk"));
        var expectedDecompileDirOk = Path.GetFullPath(Path.Combine(plannedOkRoot, "decompile"));
        var effectiveRootOk = Path.GetFullPath(decompiledOkDir);

        var assemblyDecompiler = new Mock<IAssemblyDecompilerService>();
        assemblyDecompiler
            .Setup(d => d.DecompileToProjectDirectory(
                Path.GetFullPath(dllFail),
                plannedFailDecompile,
                It.IsAny<CancellationToken>()))
            .Throws(new AggregateException(
                new InvalidOperationException("Error decompiling for 'a.cs'"),
                new InvalidOperationException("Error decompiling for 'b.cs'")));
        assemblyDecompiler
            .Setup(d => d.DecompileToProjectDirectory(
                Path.GetFullPath(dllOk),
                expectedDecompileDirOk,
                It.IsAny<CancellationToken>()))
            .Returns(effectiveRootOk);

        var sequence = new MockSequence();
        var solutionDiscovery = new Mock<ISolutionDiscoveryService>(MockBehavior.Strict);
        solutionDiscovery.InSequence(sequence)
            .Setup(s => s.GetSolutionName(effectiveRootOk))
            .Returns(ExtractionResult<string>.Success("AsmOk"));
        solutionDiscovery.InSequence(sequence)
            .Setup(s => s.FindProjects(effectiveRootOk))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([projectOk]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery
            .Setup(f => f.FindSolutionDocs(effectiveRootOk, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(projectOk, It.IsAny<string>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([csOk]));
        fileDiscovery
            .Setup(f => f.FindUnmappedDirectories(effectiveRootOk, It.IsAny<IReadOnlyList<ProjectDefinition>>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<(string, List<string>)>>.Success([]));

        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            assemblyDecompiler.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiViewSp.GetRequiredService<IMultiViewExportService>(),
            multiViewSp.GetRequiredService<IMultiViewReadmeMarkdownGenerator>(),
            TestAppSettingsFactory.Default(),
            [post.Object]);

        var markerPath = Path.Combine(export.Root, MultiViewExportPaths.SafetyMarkerFileName);
        await File.WriteAllTextAsync(markerPath, "previous run", TestContext.Current.CancellationToken);

        var allAssemblySourcesOk = await sut.RunAsync([dllFail, dllOk], export.Root);
        Assert.False(allAssemblySourcesOk);

        assemblyDecompiler.Verify(
            d => d.DecompileToProjectDirectory(
                Path.GetFullPath(dllFail),
                plannedFailDecompile,
                It.IsAny<CancellationToken>()),
            Times.Once);
        assemblyDecompiler.Verify(
            d => d.DecompileToProjectDirectory(
                Path.GetFullPath(dllOk),
                expectedDecompileDirOk,
                It.IsAny<CancellationToken>()),
            Times.Once);
        solutionDiscovery.Verify(s => s.GetSolutionName(effectiveRootOk), Times.Once);
        solutionDiscovery.Verify(s => s.FindProjects(effectiveRootOk), Times.Once);

        post.Verify(p => p.ExecuteAsync("AsmOk", plannedOkRoot), Times.Once);
    }

    [Fact]
    public async Task RunAsync_with_directory_input_does_not_invoke_assembly_decompiler()
    {
        using var export = new TempWorkspace();
        using var solution = new TempWorkspace();
        using var multiViewSp = CreateMultiViewServiceProvider();

        var projPath = Path.Combine(solution.Root, "P1", "P1.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(projPath)!);
        await File.WriteAllTextAsync(
            projPath,
            """<Project Sdk="Microsoft.NET.Sdk"></Project>""",
            TestContext.Current.CancellationToken);
        var csPath = Path.Combine(solution.Root, "P1", "A.cs");
        await File.WriteAllTextAsync(csPath, "namespace N; public class A { }", TestContext.Current.CancellationToken);

        var project = new ProjectDefinition("P1", projPath);
        var solutionDiscovery = new Mock<ISolutionDiscoveryService>();
        solutionDiscovery
            .Setup(s => s.GetSolutionName(solution.Root))
            .Returns(ExtractionResult<string>.Success("MySol"));
        solutionDiscovery
            .Setup(s => s.FindProjects(solution.Root))
            .Returns(ExtractionResult<List<ProjectDefinition>>.Success([project]));

        var fileDiscovery = new Mock<IFileDiscoveryService>();
        fileDiscovery
            .Setup(f => f.FindSolutionDocs(solution.Root, It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([]));
        fileDiscovery
            .Setup(f => f.FindFilesForProject(project, It.IsAny<string>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<string>>.Success([csPath]));
        fileDiscovery
            .Setup(f => f.FindUnmappedDirectories(solution.Root, It.IsAny<IReadOnlyList<ProjectDefinition>>(), It.IsAny<AppSettings>()))
            .Returns(ExtractionResult<List<(string, List<string>)>>.Success([]));

        var assemblyDecompiler = new Mock<IAssemblyDecompilerService>(MockBehavior.Strict);
        var post = new Mock<IPostExportTask>();
        post.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        var sut = new ConsoleOrchestrator(
            solutionDiscovery.Object,
            fileDiscovery.Object,
            assemblyDecompiler.Object,
            new CsprojDependencyGraphMarkdownGenerator(),
            multiViewSp.GetRequiredService<IMultiViewExportService>(),
            multiViewSp.GetRequiredService<IMultiViewReadmeMarkdownGenerator>(),
            TestAppSettingsFactory.Default(),
            [post.Object]);

        var markerPath = Path.Combine(export.Root, MultiViewExportPaths.SafetyMarkerFileName);
        await File.WriteAllTextAsync(markerPath, "previous run", TestContext.Current.CancellationToken);

        await sut.RunAsync([solution.Root], export.Root);

        assemblyDecompiler.Verify(
            d => d.DecompileToProjectDirectory(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
