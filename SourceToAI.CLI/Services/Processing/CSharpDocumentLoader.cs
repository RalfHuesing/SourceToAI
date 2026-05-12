using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceToAI.CLI.Models;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

public sealed class CSharpDocumentLoader : ICSharpDocumentLoader
{
    private readonly ConcurrentDictionary<string, Lazy<CachedCSharpParse>> _parseCache = new(StringComparer.OrdinalIgnoreCase);

    public void Clear() => _parseCache.Clear();

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

                var lazyParse = _parseCache.GetOrAdd(
                    fullPath,
                    key => new Lazy<CachedCSharpParse>(
                        () => ReadAndParse(key),
                        LazyThreadSafetyMode.ExecutionAndPublication));

                var cached = lazyParse.Value;

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
        var sourceText = File.ReadAllText(fullPath);
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
