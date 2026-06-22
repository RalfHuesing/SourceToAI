using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SourceToAI.Tests.Processing;

public class ProjectSplittingEngineTests
{
    [Fact]
    public void PartitionProject_inactive_splitting_returns_single_partition_with_all_files()
    {
        using var ws = new TempWorkspace();
        var p1 = ws.WriteFile("A.cs", "namespace N1; class A { }");
        var p2 = ws.WriteFile("B.json", "{}");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [p1, p2], new ProjectSplittingOptions(0, 0));

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].SubNamespaceName);
        Assert.Equal(2, result[0].Paths.Count);
    }

    [Fact]
    public void PartitionProject_separates_assets_correctly()
    {
        using var ws = new TempWorkspace();
        var csFile = ws.WriteFile("A.cs", "namespace N1; class A { }");
        var jsonFile = ws.WriteFile("B.json", "{}");
        var sqlFile = ws.WriteFile("C.sql", "SELECT 1;");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        // Active splitting
        var result = sut.PartitionProject(project, [csFile, jsonFile, sqlFile], new ProjectSplittingOptions(100, 5));

        // Should have 2 partitions: one for "N1" (C#) and one for "_Assets" (non-C#)
        Assert.Equal(2, result.Count);
        
        var csPartition = result.First(r => r.SubNamespaceName == "N1");
        Assert.Single(csPartition.Paths);
        Assert.Equal(csFile, csPartition.Paths[0]);

        var assetPartition = result.First(r => r.SubNamespaceName == "_Assets");
        Assert.Equal(2, assetPartition.Paths.Count);
        Assert.Contains(jsonFile, assetPartition.Paths);
        Assert.Contains(sqlFile, assetPartition.Paths);
    }

    [Fact]
    public void PartitionProject_separates_global_namespace_into_core_when_not_suppressed()
    {
        using var ws = new TempWorkspace();
        var globalFile = ws.WriteFile("Program.cs", "class Program { }"); // No namespace
        var nsFile = ws.WriteFile("A.cs", "namespace MyCompany.Features; class A { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [globalFile, nsFile], new ProjectSplittingOptions(100, 5, SuppressCorePartition: false));

        Assert.Equal(2, result.Count);
        
        var corePartition = result.First(r => r.SubNamespaceName == "Core");
        Assert.Single(corePartition.Paths);
        Assert.Equal(globalFile, corePartition.Paths[0]);

        var featurePartition = result.First(r => r.SubNamespaceName == "MyCompany.Features");
        Assert.Single(featurePartition.Paths);
        Assert.Equal(nsFile, featurePartition.Paths[0]);
    }

    [Fact]
    public void PartitionProject_absorbs_core_into_smallest_namespace_partition()
    {
        using var ws = new TempWorkspace();
        var globalFile = ws.WriteFile("Program.cs", "class Program { }");
        var smallNsFile = ws.WriteFile("Small.cs", "namespace MyCompany.Small; class S { }");
        var largeNsFile = ws.WriteFile("Large.cs", "namespace MyCompany.Large; class L { " + new string('x', 40 * 1024) + " }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [globalFile, smallNsFile, largeNsFile], new ProjectSplittingOptions(100, 5));

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, r => r.SubNamespaceName == "Core");

        var smallPartition = result.First(r => r.SubNamespaceName == "MyCompany.Small");
        Assert.Equal(2, smallPartition.Paths.Count);
        Assert.Contains(globalFile, smallPartition.Paths);
        Assert.Contains(smallNsFile, smallPartition.Paths);

        var largePartition = result.First(r => r.SubNamespaceName == "MyCompany.Large");
        Assert.Single(largePartition.Paths);
        Assert.Equal(largeNsFile, largePartition.Paths[0]);
    }

    [Fact]
    public void PartitionProject_absorbs_core_only_project_into_single_partition()
    {
        using var ws = new TempWorkspace();
        var globalFile = ws.WriteFile("Program.cs", "class Program { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [globalFile], new ProjectSplittingOptions(100, 5));

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].SubNamespaceName);
        Assert.Single(result[0].Paths);
        Assert.Equal(globalFile, result[0].Paths[0]);
    }

    [Fact]
    public void PartitionProject_collapses_namespaces_to_enforce_hard_maxFileCount_limit()
    {
        using var ws = new TempWorkspace();
        var f1 = ws.WriteFile("Bookings.cs", "namespace MyCompany.Features.Bookings; class Bookings { }");
        var f2 = ws.WriteFile("Billing.cs", "namespace MyCompany.Features.Billing; class Billing { }");
        // Make f3 very large so combined size exceeds the soft limit
        var f3 = ws.WriteFile("Auth.cs", "namespace MyCompany.Auth; class Auth { " + new string('x', 60 * 1024) + " }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        // We have 3 namespaces, but we only want at most 2 files, soft limit 50 KB
        var result = sut.PartitionProject(project, [f1, f2, f3], new ProjectSplittingOptions(50, 2));

        // Must enforce hard limit: result count <= 2
        Assert.True(result.Count <= 2, $"Expected at most 2 partitions, but got {result.Count}");

        // Bookings and Billing are sibling nodes (under MyCompany.Features) and have smaller total size than Auth.
        // Therefore, MyCompany.Features.Bookings and MyCompany.Features.Billing should be collapsed into MyCompany.Features.
        var featuresPartition = result.First(r => r.SubNamespaceName == "MyCompany.Features");
        Assert.Equal(2, featuresPartition.Paths.Count);
        Assert.Contains(f1, featuresPartition.Paths);
        Assert.Contains(f2, featuresPartition.Paths);

        var authPartition = result.First(r => r.SubNamespaceName == "MyCompany.Auth");
        Assert.Single(authPartition.Paths);
        Assert.Equal(f3, authPartition.Paths[0]);
    }

    [Fact]
    public void PartitionProject_optimizes_small_siblings_by_merging_them_under_soft_limit()
    {
        using var ws = new TempWorkspace();
        var f1 = ws.WriteFile("Bookings.cs", "namespace MyCompany.Features.Bookings; class Bookings { }");
        var f2 = ws.WriteFile("Billing.cs", "namespace MyCompany.Features.Billing; class Billing { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        // We allow up to 5 files (no hard pressure to merge), but the size is small (e.g. 500 KB limit),
        // so sibling optimization should merge MyCompany.Features.Bookings and MyCompany.Features.Billing into MyCompany.Features.
        var result = sut.PartitionProject(project, [f1, f2], new ProjectSplittingOptions(500, 5));

        // They are siblings under MyCompany.Features and combined size is way below 500 KB.
        // Sibling optimization should merge them into MyCompany.Features.
        Assert.Single(result);
        Assert.Equal("MyCompany.Features", result[0].SubNamespaceName);
        Assert.Equal(2, result[0].Paths.Count);
    }

    [Fact]
    public void PartitionProject_groups_razor_with_code_behind()
    {
        using var ws = new TempWorkspace();
        var pagesDir = Path.Combine(ws.Root, "Pages");
        Directory.CreateDirectory(pagesDir);
        var razor = ws.WriteFile("Pages/Home.razor", "<h1>Home</h1>");
        var codeBehind = ws.WriteFile("Pages/Home.razor.cs", "namespace MyApp.Pages; partial class Home { }");
        var scopedCss = ws.WriteFile("Pages/Home.razor.css", ".home { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [razor, codeBehind, scopedCss], new ProjectSplittingOptions(100, 5));

        Assert.Single(result);
        Assert.Equal("MyApp.Pages", result[0].SubNamespaceName);
        Assert.Equal(3, result[0].Paths.Count);
        Assert.Contains(Path.GetFullPath(razor), result[0].Paths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFullPath(codeBehind), result[0].Paths, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(Path.GetFullPath(scopedCss), result[0].Paths, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void PartitionProject_collapses_core_under_maxFileCount()
    {
        using var ws = new TempWorkspace();
        var globalFile = ws.WriteFile("Program.cs", "class Program { }");
        var nsFile = ws.WriteFile("A.cs", "namespace N1; class A { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [globalFile, nsFile], new ProjectSplittingOptions(100, 1));

        Assert.Single(result);
        Assert.Equal("N1", result[0].SubNamespaceName);
        Assert.Equal(2, result[0].Paths.Count);
        Assert.Contains(globalFile, result[0].Paths);
        Assert.Contains(nsFile, result[0].Paths);
        Assert.DoesNotContain(result, r => r.SubNamespaceName == "Core");
    }

    [Fact]
    public void PartitionProject_with_suppressed_core_uses_full_maxFileCount()
    {
        using var ws = new TempWorkspace();
        var globalFile = ws.WriteFile("Program.cs", "class Program { }");
        var paths = new List<string> { globalFile };
        for (var i = 1; i <= 8; i++)
        {
            paths.Add(ws.WriteFile($"N{i}.cs", $"namespace N{i}; class C{i} {{ }}"));
        }

        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));
        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, paths, new ProjectSplittingOptions(500, 8));

        Assert.Equal(8, result.Count);
        Assert.DoesNotContain(result, r => r.SubNamespaceName == "Core");
        Assert.True(
            result.Any(r => r.Paths.Contains(globalFile)),
            "Program.cs soll in einer Namespace-Partition landen");
    }

    [Fact]
    public void PartitionProject_keeps_assets_separate_under_pressure()
    {
        using var ws = new TempWorkspace();
        var csFile = ws.WriteFile("A.cs", "namespace N1; class A { }");
        var jsonFile = ws.WriteFile("data.json", "{}");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [csFile, jsonFile], new ProjectSplittingOptions(100, 1));

        Assert.Equal(2, result.Count);
        Assert.Single(result, r => r.SubNamespaceName == "N1");
        Assert.Single(result, r => r.SubNamespaceName == "_Assets");
        Assert.Contains(jsonFile, result.First(r => r.SubNamespaceName == "_Assets").Paths);
    }
}
