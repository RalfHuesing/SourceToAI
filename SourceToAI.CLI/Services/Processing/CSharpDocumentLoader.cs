using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.IO;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

public sealed class CSharpDocumentLoader(IFileReader fileReader) : ICSharpDocumentLoader
{
    public ExtractionResult<IReadOnlyList<ParsedCSharpDocument>> LoadParsedDocuments(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder)
    {
        try
        {
            var documents = new List<ParsedCSharpDocument>();
            var seenFullPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in absoluteFilePathsInDisplayOrder)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = Path.GetFullPath(path);
                if (!seenFullPaths.Add(fullPath))
                    continue;

                var sourceText = fileReader.ReadAllText(fullPath);
                var sizeBytes = Encoding.UTF8.GetByteCount(sourceText);
                var syntaxTree = CSharpSyntaxTree.ParseText(
                    sourceText,
                    CSharpParseOptions.Default,
                    path: fullPath,
                    encoding: Encoding.UTF8);

                var parseErrors = syntaxTree.GetDiagnostics()
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToList();

                var relativePath = Path.GetRelativePath(project.RootDirectory, fullPath);
                documents.Add(new ParsedCSharpDocument(
                    fullPath,
                    relativePath,
                    sourceText,
                    syntaxTree,
                    sizeBytes,
                    parseErrors));
            }

            return ExtractionResult<IReadOnlyList<ParsedCSharpDocument>>.Success(documents);
        }
        catch (Exception ex)
        {
            return ExtractionResult<IReadOnlyList<ParsedCSharpDocument>>.Failure(
                $"Fehler beim Laden von C#-Dateien für {project.ProjectName}: {ex.Message}");
        }
    }
}
