using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.IO;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

public sealed class CSharpDocumentLoader(IFileReader fileReader) : ICSharpDocumentLoader
{
    private readonly Dictionary<string, CachedCSharpParse> _parseCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    public void Clear()
    {
        lock (_sync)
            _parseCache.Clear();
    }

    public ExtractionResult<IReadOnlyList<ParsedCSharpDocument>> LoadParsedDocuments(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder)
    {
        try
        {
            var documents = new List<ParsedCSharpDocument>();
            var seenInThisInvocation = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in absoluteFilePathsInDisplayOrder)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = Path.GetFullPath(path);
                if (!seenInThisInvocation.Add(fullPath))
                    continue;

                CachedCSharpParse cached;
                lock (_sync)
                {
                    if (_parseCache.TryGetValue(fullPath, out var existing))
                    {
                        cached = existing;
                    }
                    else
                    {
                        cached = ReadAndParse(fullPath);
                        _parseCache[fullPath] = cached;
                    }
                }

                var relativePath = Path.GetRelativePath(project.RootDirectory, fullPath);
                documents.Add(new ParsedCSharpDocument(
                    fullPath,
                    relativePath,
                    cached.SourceText,
                    cached.SyntaxTree,
                    cached.SizeBytes,
                    cached.ParseErrorMessages));
            }

            return ExtractionResult<IReadOnlyList<ParsedCSharpDocument>>.Success(documents);
        }
        catch (Exception ex)
        {
            return ExtractionResult<IReadOnlyList<ParsedCSharpDocument>>.Failure(
                $"Fehler beim Laden von C#-Dateien für {project.ProjectName}: {ex.Message}");
        }
    }

    private CachedCSharpParse ReadAndParse(string fullPath)
    {
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

        return new CachedCSharpParse(sourceText, syntaxTree, sizeBytes, parseErrors);
    }

    private sealed record CachedCSharpParse(
        string SourceText,
        SyntaxTree SyntaxTree,
        long SizeBytes,
        IReadOnlyList<string> ParseErrorMessages);
}
