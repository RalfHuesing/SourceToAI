using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services;
using System.Text;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Parse-Cache: pro normalisiertem absoluten Pfad höchstens eine Materialisierung von
/// <see cref="ReadAndParse"/> unter Parallelität (<see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>).
/// </summary>
/// <remarks>
/// Skippable I/O-Fehler: <see cref="Lazy"/> speichert Factory-Ausnahmen — bei
/// <see cref="SkippableLocalFileIoExceptions"/> ersetzen wir den betroffenen Cache-Eintrag per
/// <see cref="ConcurrentDictionary{TKey,TValue}.TryUpdate"/> durch eine frische <see cref="Lazy{T}"/>,
/// sobald der gescheiterte Wert noch der veröffentlichte ist (kein Fremd-Eintrag überschreiben).
/// </remarks>
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

                var lazyParse = _parseCache.GetOrAdd(
                    fullPath,
                    fp => new Lazy<CachedCSharpParse>(
                        () => ReadAndParse(fp),
                        LazyThreadSafetyMode.ExecutionAndPublication));

                CachedCSharpParse cached;
                try
                {
                    cached = lazyParse.Value;
                }
                catch (Exception ex) when (SkippableLocalFileIoExceptions.Matches(ex))
                {
                    var retryLazy = new Lazy<CachedCSharpParse>(
                        () => ReadAndParse(fullPath),
                        LazyThreadSafetyMode.ExecutionAndPublication);
                    _ = _parseCache.TryUpdate(fullPath, retryLazy, lazyParse);
                    warnings.Add(
                        $"„{fullPath}“ übersprungen ({ex.GetType().Name}): {ex.Message}");
                    continue;
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
