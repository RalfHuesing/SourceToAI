using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.App;

/// <summary>
/// Task 03: mehrere Projekte und wiederholte Läufe — Ausgabe muss trotz paralleler View-Erzeugung bit-identisch bleiben
/// (keine Korruption der Dateiliste / Stammvergabe).
/// </summary>
public sealed class MultiViewExportParallelDeterminismTests
{
    private static readonly string[] ViewFolders =
        ["complete", "signatures-only", "public-only", "dto-only"];

    [Fact]
    public void WriteMergedSolutionViews_parallel_build_phase_is_deterministic_across_runs()
    {
        using var solution = new TempWorkspace();
        using var serviceProvider = MultiViewExportTestHost.CreateServiceProvider();
        var exportService = serviceProvider.GetRequiredService<IMultiViewExportService>();

        const string solutionDisplayName = "ParSol";
        const string tfm = "net10.0";
        var orderedNames = new[] { "Alpha", "Beta", "Delta", "Gamma", "Zeta" };

        var byName = new Dictionary<string, (ProjectDefinition Project, string CsPath)>(StringComparer.Ordinal);
        foreach (var name in orderedNames)
        {
            solution.WriteFile(
                $"{name}/{name}.csproj",
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>{tfm}</TargetFramework>
                  </PropertyGroup>
                </Project>
                """);
            var csPath = solution.WriteFile(
                $"{name}/Types.cs",
                $$"""
                namespace Fixture.{{name}};

                public static class {{name}}Marker { }
                """);
            var csproj = Path.Combine(solution.Root, name, $"{name}.csproj");
            byName[name] = (new ProjectDefinition(name, csproj), csPath);
        }

        // Eingabe absichtlich nicht alphabetisch — der Service sortiert intern; Parallelität darf das Ergebnis nicht wackeln.
        var shuffled = new[] { "Zeta", "Gamma", "Alpha", "Delta", "Beta" }
            .Select(n => (byName[n].Project, (IReadOnlyList<string>)new[] { byName[n].CsPath }))
            .ToList();

        var sessionId = Guid.Parse("00000000-0000-4000-8000-0000000000aa");
        var generated = new DateTimeOffset(2026, 5, 12, 12, 0, 0, TimeSpan.Zero);

        string? referenceFingerprint = null;
        for (var run = 0; run < 8; run++)
        {
            using var export = new TempWorkspace();
            var outputRoot = export.Root;
            exportService.WriteMergedSolutionViews(
                outputRoot,
                solutionDisplayName,
                solution.Root,
                sessionId,
                generated,
                shuffled,
                solutionDocumentationAbsolutePaths: null);

            foreach (var name in orderedNames)
            {
                Assert.True(
                    File.Exists(Path.Combine(outputRoot, "Merged", "complete", $"{solutionDisplayName}.{name}-complete.md")),
                    $"Run {run}: Merged/complete/{solutionDisplayName}.{name}-complete.md");
            }

            var fingerprint = FingerprintExportTree(outputRoot);
            referenceFingerprint ??= fingerprint;
            Assert.Equal(referenceFingerprint, fingerprint);
        }
    }

    private static string FingerprintExportTree(string outputRoot)
    {
        var sb = new StringBuilder();
        foreach (var viewFolder in ViewFolders)
        {
            var dir = Path.Combine(outputRoot, "Merged", viewFolder);
            if (!Directory.Exists(dir))
                continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*.md").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                sb.Append(viewFolder);
                sb.Append('|');
                sb.Append(Path.GetFileName(file));
                sb.Append('|');
                sb.AppendLine(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))));
            }
        }

        return sb.ToString();
    }
}
