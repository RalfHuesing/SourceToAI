#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Service zur lösungzweiten Budgetierung und Konsolidierung von Export-Einheiten,
/// um den Parameter --max-file-count strikt einzuhalten.
/// </summary>
public sealed class SolutionExportUnitConsolidator
{
    public Dictionary<string, int> CalculatePerProjectLimits(
        int maxFileCount,
        bool hasSolutionDocs,
        int unmappedDirsCount,
        IReadOnlyList<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> projectsWithFiles)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (maxFileCount <= 0)
        {
            foreach (var p in projectsWithFiles)
                result[p.Project.ProjectName] = 0;
            return result;
        }

        var activeProjects = projectsWithFiles
            .Where(p => p.AbsoluteFilePaths.Count > 0)
            .ToList();

        if (activeProjects.Count == 0)
            return result;

        int fixedUnits = (hasSolutionDocs ? 1 : 0) + unmappedDirsCount;
        int availableForProjects = maxFileCount - fixedUnits;

        if (availableForProjects <= activeProjects.Count)
        {
            foreach (var p in activeProjects)
                result[p.Project.ProjectName] = 1;
            return result;
        }

        DistributeExtraBudget(result, activeProjects, availableForProjects);
        return result;
    }

    private static void DistributeExtraBudget(
        Dictionary<string, int> result,
        List<(ProjectDefinition Project, IReadOnlyList<string> AbsoluteFilePaths)> activeProjects,
        int availableForProjects)
    {
        foreach (var p in activeProjects)
            result[p.Project.ProjectName] = 1;

        int extraBudget = availableForProjects - activeProjects.Count;
        long totalFiles = activeProjects.Sum(p => (long)p.AbsoluteFilePaths.Count);

        if (totalFiles == 0)
            return;

        var remainderList = new List<(string ProjectName, double DesiredExtra)>();
        int allocatedExtraSum = 0;

        foreach (var p in activeProjects)
        {
            double share = (double)p.AbsoluteFilePaths.Count / totalFiles * extraBudget;
            int alloc = (int)Math.Floor(share);
            result[p.Project.ProjectName] += alloc;
            allocatedExtraSum += alloc;
            remainderList.Add((p.Project.ProjectName, share - alloc));
        }

        int remainingToDistribute = extraBudget - allocatedExtraSum;
        foreach (var item in remainderList.OrderByDescending(r => r.DesiredExtra).Take(remainingToDistribute))
        {
            result[item.ProjectName] += 1;
        }
    }

    public List<ExportUnit> ConsolidateExportUnits(
        List<ExportUnit> units,
        int maxFileCount,
        string solutionRootPath,
        HashSet<string> unmappedProjectNames,
        Dictionary<string, (string RealProj, string SubNamespace)> virtualProjectSplitInfo)
    {
        if (maxFileCount <= 0 || units.Count <= maxFileCount)
            return units;

        var currentUnits = units.ToList();
        currentUnits = ConsolidateUnmappedUnits(currentUnits, solutionRootPath, unmappedProjectNames);

        if (currentUnits.Count <= maxFileCount)
            return currentUnits;

        return MergeSmallestUnitsUntilLimit(currentUnits, maxFileCount, solutionRootPath, virtualProjectSplitInfo);
    }

    private static List<ExportUnit> ConsolidateUnmappedUnits(
        List<ExportUnit> units,
        string solutionRootPath,
        HashSet<string> unmappedProjectNames)
    {
        var unmappedUnits = units
            .Where(u => unmappedProjectNames.Contains(u.Project.ProjectName))
            .ToList();

        if (unmappedUnits.Count <= 1)
            return units;

        var remaining = units
            .Where(u => !unmappedProjectNames.Contains(u.Project.ProjectName))
            .ToList();

        var combinedPaths = unmappedUnits
            .SelectMany(u => u.Paths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var virtualCsproj = Path.Combine(solutionRootPath, "_Unmapped", "virtual.csproj");
        var mergedProject = new ProjectDefinition("_Unmapped", virtualCsproj);
        remaining.Add(new ExportUnit(mergedProject, combinedPaths, true));

        return remaining;
    }

    private static List<ExportUnit> MergeSmallestUnitsUntilLimit(
        List<ExportUnit> units,
        int maxFileCount,
        string solutionRootPath,
        Dictionary<string, (string RealProj, string SubNamespace)> virtualProjectSplitInfo)
    {
        var current = units.ToList();

        while (current.Count > maxFileCount && current.Count > 1)
        {
            var mergeCandidates = current
                .Where(u => !string.Equals(u.Project.ProjectName, ".Docs", StringComparison.OrdinalIgnoreCase))
                .OrderBy(u => u.Paths.Count)
                .ThenBy(u => u.Project.ProjectName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (mergeCandidates.Count < 2)
            {
                mergeCandidates = current
                    .OrderBy(u => u.Paths.Count)
                    .ThenBy(u => u.Project.ProjectName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var u1 = mergeCandidates[0];
            var u2 = mergeCandidates[1];

            current.Remove(u1);
            current.Remove(u2);

            var mergedUnit = CreateMergedUnit(u1, u2, solutionRootPath, virtualProjectSplitInfo);
            current.Add(mergedUnit);
        }

        return current;
    }

    private static ExportUnit CreateMergedUnit(
        ExportUnit u1,
        ExportUnit u2,
        string solutionRootPath,
        Dictionary<string, (string RealProj, string SubNamespace)> virtualProjectSplitInfo)
    {
        string solutionName = Path.GetFileName(solutionRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string combinedName = BuildCombinedName(u1.Project.ProjectName, u2.Project.ProjectName, solutionName);
        var combinedPaths = u1.Paths.Concat(u2.Paths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var virtualCsproj = Path.Combine(solutionRootPath, $"{combinedName}.virtual.csproj");
        var mergedProject = new ProjectDefinition(combinedName, virtualCsproj);

        virtualProjectSplitInfo[combinedName] = (combinedName, string.Empty);
        bool docsOnly = u1.DocsOnlyInCompleteView && u2.DocsOnlyInCompleteView;

        return new ExportUnit(mergedProject, combinedPaths, docsOnly);
    }

    private static string BuildCombinedName(string name1, string name2, string solutionName)
    {
        string s1 = StripSolutionPrefix(name1, solutionName);
        string s2 = StripSolutionPrefix(name2, solutionName);

        string combinedShort = FormatCombinedShortName(s1, s2);
        if (combinedShort.Length > 40)
        {
            combinedShort = TruncateOrGroupNames(s1, s2);
        }

        return string.IsNullOrEmpty(solutionName) ? combinedShort : $"{solutionName}.{combinedShort}";
    }

    private static string StripSolutionPrefix(string name, string solutionName)
    {
        var clean = name.StartsWith('_') ? name.TrimStart('_') : name;
        if (!string.IsNullOrEmpty(solutionName))
        {
            var prefixDot = solutionName + ".";
            if (clean.StartsWith(prefixDot, StringComparison.OrdinalIgnoreCase))
                return clean.Substring(prefixDot.Length);

            var prefixUnderscore = solutionName + "_";
            if (clean.StartsWith(prefixUnderscore, StringComparison.OrdinalIgnoreCase))
                return clean.Substring(prefixUnderscore.Length);

            if (string.Equals(clean, solutionName, StringComparison.OrdinalIgnoreCase))
                return "Core";
        }
        return clean;
    }

    private static string FormatCombinedShortName(string s1, string s2)
    {
        int dot1 = s1.IndexOf('.');
        int dot2 = s2.IndexOf('.');
        if (dot1 > 0 && dot2 > 0)
        {
            string p1 = s1.Substring(0, dot1);
            string p2 = s2.Substring(0, dot2);
            if (string.Equals(p1, p2, StringComparison.OrdinalIgnoreCase))
            {
                string rest1 = s1.Substring(dot1 + 1);
                string rest2 = s2.Substring(dot2 + 1);
                return $"{p1}_{rest1}_{rest2}";
            }
        }
        return $"{s1}_{s2}";
    }

    private static string TruncateOrGroupNames(string s1, string s2)
    {
        int dot1 = s1.IndexOf('.');
        int dot2 = s2.IndexOf('.');
        if (dot1 > 0 && dot2 > 0)
        {
            string p1 = s1.Substring(0, dot1);
            string p2 = s2.Substring(0, dot2);
            if (string.Equals(p1, p2, StringComparison.OrdinalIgnoreCase))
            {
                return $"{p1}_Grouped";
            }
        }

        string head1 = dot1 > 0 ? s1.Substring(0, dot1) : s1;
        string head2 = dot2 > 0 ? s2.Substring(0, dot2) : s2;
        string candidate = $"{head1}_{head2}";
        return candidate.Length > 30 ? "Modules_Grouped" : candidate;
    }
}
