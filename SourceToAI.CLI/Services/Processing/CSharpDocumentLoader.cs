using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services;
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
            var warnings = new List<string>();
            var seenInThisInvocation = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var path in absoluteFilePathsInDisplayOrder)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = Path.GetFullPath(path);
                if (!seenInThisInvocation.Add(fullPath))
                    continue;

                if (!_parseCache.TryGetValue(fullPath, out var lazyParse))
                {
                    CachedCSharpParse parsed;
                    try
                    {
                        parsed = ReadAndParse(fullPath);
                    }
                    catch (Exception ex) when (SkippableLocalFileIoExceptions.Matches(ex))
                    {
                        warnings.Add(
                            $"„{fullPath}“ übersprungen ({ex.GetType().Name}): {ex.Message}");
                        continue;
                    }

                    lazyParse = new Lazy<CachedCSharpParse>(
                        () => parsed,
                        LazyThreadSafetyMode.ExecutionAndPublication);

                    if (!_parseCache.TryAdd(fullPath, lazyParse))
                        lazyParse = _parseCache[fullPath];
                }

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

            return ExtractionResult<IReadOnlyList<ParsedCSharpDocument>>.Success(
                documents,
                warnings.Count > 0 ? warnings : null);
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
