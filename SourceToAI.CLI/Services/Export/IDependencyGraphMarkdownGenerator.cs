using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Export;

/// <summary>
/// Erzeugt Markdown für <c>dependency-graph.md</c> aus allen <see cref="ProjectDefinition"/>-Einträgen
/// (Analyse der jeweiligen <c>.csproj</c>).
/// </summary>
public interface IDependencyGraphMarkdownGenerator
{
    /// <param name="solutionRoot">Wurzel der Solution (für relative Pfade in der Ausgabe).</param>
    ExtractionResult<string> Generate(string solutionRoot, IReadOnlyList<ProjectDefinition> projects);
}
