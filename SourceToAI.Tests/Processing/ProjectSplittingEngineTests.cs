using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.Tests.Support;
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

        var result = sut.PartitionProject(project, [p1, p2], 0, 0);

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
        var result = sut.PartitionProject(project, [csFile, jsonFile, sqlFile], 100, 5);

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
    public void PartitionProject_separates_global_namespace_into_core()
    {
        using var ws = new TempWorkspace();
        var globalFile = ws.WriteFile("Program.cs", "class Program { }"); // No namespace
        var nsFile = ws.WriteFile("A.cs", "namespace MyCompany.Features; class A { }");
        var project = new ProjectDefinition("App", Path.Combine(ws.Root, "App.csproj"));

        var loader = new CSharpDocumentLoader();
        var sut = new ProjectSplittingEngine(loader);

        var result = sut.PartitionProject(project, [globalFile, nsFile], 100, 5);

        Assert.Equal(2, result.Count);
        
        var corePartition = result.First(r => r.SubNamespaceName == "Core");
        Assert.Single(corePartition.Paths);
        Assert.Equal(globalFile, corePartition.Paths[0]);

        var featurePartition = result.First(r => r.SubNamespaceName == "MyCompany.Features");
        Assert.Single(featurePartition.Paths);
        Assert.Equal(nsFile, featurePartition.Paths[0]);
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
        var result = sut.PartitionProject(project, [f1, f2, f3], 50, 2);

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
        var result = sut.PartitionProject(project, [f1, f2], 500, 5);

        // They are siblings under MyCompany.Features and combined size is way below 500 KB.
        // Sibling optimization should merge them into MyCompany.Features.
        Assert.Single(result);
        Assert.Equal("MyCompany.Features", result[0].SubNamespaceName);
        Assert.Equal(2, result[0].Paths.Count);
    }
}
