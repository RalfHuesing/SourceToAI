#nullable enable
using System.Collections.Generic;
using System.IO;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Export;
using Xunit;

namespace SourceToAI.Tests.Export;

public sealed class SolutionExportUnitConsolidatorTests
{
    private readonly SolutionExportUnitConsolidator _sut = new();

    [Fact]
    public void CalculatePerProjectLimits_with_zero_max_returns_zero()
    {
        var proj = new ProjectDefinition("P1", "C:/P1.csproj");
        var list = new List<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)>
        {
            (proj, ["C:/file1.cs"])
        };

        var result = _sut.CalculatePerProjectLimits(0, false, 0, list);
        Assert.Equal(0, result["P1"]);
    }

    [Fact]
    public void CalculatePerProjectLimits_distributes_headroom_proportionally()
    {
        var p1 = new ProjectDefinition("P1", "C:/P1.csproj");
        var p2 = new ProjectDefinition("P2", "C:/P2.csproj");

        var list = new List<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)>
        {
            (p1, ["f1.cs", "f2.cs", "f3.cs"]),
            (p2, ["f4.cs"])
        };

        // maxFileCount = 5. fixed = 0. available = 5.
        // base = 2 projects. extra = 3.
        var result = _sut.CalculatePerProjectLimits(5, false, 0, list);
        Assert.True(result["P1"] > result["P2"]);
        Assert.True(result["P1"] + result["P2"] <= 5);
    }

    [Fact]
    public void ConsolidateExportUnits_reduces_unmapped_and_merges_units_to_not_exceed_limit()
    {
        string solutionRoot = "C:/Solution";
        var unmappedNames = new HashSet<string> { "Unmapped1", "Unmapped2" };
        var virtualInfo = new Dictionary<string, (string RealProj, string SubNamespace)>();

        var u1 = new ExportUnit(new ProjectDefinition("P1", "C:/P1.csproj"), ["f1.cs"], false);
        var u2 = new ExportUnit(new ProjectDefinition("P2", "C:/P2.csproj"), ["f2.cs"], false);
        var um1 = new ExportUnit(new ProjectDefinition("Unmapped1", "C:/Unmapped1/virtual.csproj"), ["u1.json"], true);
        var um2 = new ExportUnit(new ProjectDefinition("Unmapped2", "C:/Unmapped2/virtual.csproj"), ["u2.json"], true);

        var units = new List<ExportUnit> { u1, u2, um1, um2 };

        var consolidated = _sut.ConsolidateExportUnits(units, 3, solutionRoot, unmappedNames, virtualInfo);

        Assert.True(consolidated.Count <= 3);
        Assert.Contains(consolidated, u => u.Project.ProjectName == "_Unmapped");
    }
}
