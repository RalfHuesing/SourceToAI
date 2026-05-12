using System.Text;
using System.Xml.Linq;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Export;

public sealed class CsprojDependencyGraphMarkdownGenerator : IDependencyGraphMarkdownGenerator
{
    public ExtractionResult<string> Generate(string solutionRoot, IReadOnlyList<ProjectDefinition> projects)
    {
        if (string.IsNullOrWhiteSpace(solutionRoot))
            return ExtractionResult<string>.Failure("solutionRoot fehlt.");

        if (projects.Count == 0)
            return ExtractionResult<string>.Failure("Keine Projekte übergeben.");

        var solutionRootFull = Path.GetFullPath(solutionRoot);
        var sb = new StringBuilder();

        sb.AppendLine("# Dependency graph");
        sb.AppendLine();
        sb.AppendLine("NuGet- und Projekt-Referenzen je `.csproj` (nur Metadaten, kein Quellcode).");
        sb.AppendLine();

        foreach (var project in projects.OrderBy(p => p.ProjectFilePath, StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(project.ProjectFilePath))
                return ExtractionResult<string>.Failure($"Projektdatei fehlt: {project.ProjectFilePath}");

            XDocument doc;
            try
            {
                doc = XDocument.Load(project.ProjectFilePath, LoadOptions.PreserveWhitespace);
            }
            catch (Exception ex)
            {
                return ExtractionResult<string>.Failure(
                    $"Konnte `{project.ProjectFilePath}` nicht lesen: {ex.Message}");
            }

            var csprojDir = Path.GetFullPath(project.RootDirectory);
            var packages = ReadPackageReferences(doc);
            var projectRefs = ReadProjectReferences(doc, csprojDir, solutionRootFull);

            sb.AppendLine($"## {project.ProjectName}");
            sb.AppendLine();
            sb.AppendLine(
                $"Projektdatei: `{EscapeMarkdownCell(Path.GetRelativePath(solutionRootFull, Path.GetFullPath(project.ProjectFilePath)))}`");
            sb.AppendLine();

            sb.AppendLine("### NuGet (`PackageReference`)");
            sb.AppendLine();
            if (packages.Count == 0)
            {
                sb.AppendLine("*Keine PackageReference-Einträge.*");
            }
            else
            {
                sb.AppendLine("| Paket | Version |");
                sb.AppendLine("| --- | --- |");
                foreach (var row in packages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase))
                    sb.AppendLine($"| {EscapeMarkdownCell(row.Id)} | {EscapeMarkdownCell(row.Version)} |");
            }

            sb.AppendLine();
            sb.AppendLine("### Projekte (`ProjectReference`)");
            sb.AppendLine();
            if (projectRefs.Count == 0)
            {
                sb.AppendLine("*Keine ProjectReference-Einträge.*");
            }
            else
            {
                sb.AppendLine("| Referenz (relativ zur Solution) |");
                sb.AppendLine("| --- |");
                foreach (var path in projectRefs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                    sb.AppendLine($"| {EscapeMarkdownCell(path)} |");
            }

            sb.AppendLine();
        }

        return ExtractionResult<string>.Success(sb.ToString());
    }

    private static List<(string Id, string Version)> ReadPackageReferences(XDocument doc)
    {
        var list = new List<(string Id, string Version)>();
        foreach (var el in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
        {
            var id = el.Attribute("Include")?.Value?.Trim();
            if (string.IsNullOrEmpty(id))
                continue;

            var version = ReadPackageVersion(el);
            list.Add((id, version));
        }

        return list;
    }

    private static string ReadPackageVersion(XElement packageReference)
    {
        var attr = packageReference.Attribute("Version")?.Value?.Trim();
        if (!string.IsNullOrEmpty(attr))
            return attr;

        var versionChild = packageReference.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "Version")?.Value?.Trim();
        return versionChild ?? string.Empty;
    }

    private static List<string> ReadProjectReferences(XDocument doc, string csprojDir, string solutionRootFull)
    {
        var list = new List<string>();
        foreach (var el in doc.Descendants().Where(e => e.Name.LocalName == "ProjectReference"))
        {
            var include = el.Attribute("Include")?.Value?.Trim();
            if (string.IsNullOrEmpty(include))
                continue;

            var targetFull = Path.GetFullPath(Path.Combine(csprojDir, include));
            string display;
            try
            {
                display = Path.GetRelativePath(solutionRootFull, targetFull);
            }
            catch (ArgumentException)
            {
                display = include;
            }

            list.Add(display);
        }

        return list;
    }

    private static string EscapeMarkdownCell(string value) =>
        value.Replace("\\", "/").Replace("|", "\\|");
}
