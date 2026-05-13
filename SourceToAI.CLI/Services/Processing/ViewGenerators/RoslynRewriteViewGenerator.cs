using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

/// <summary>
/// View-Generator für Roslyn-Rewriter-Pipeline: <c>rewrite(root)</c> → Text →
/// <see cref="CSharpCompilationUnitExportSurface.HasExportableSurface"/>.
/// </summary>
public sealed class RoslynRewriteViewGenerator : IViewGenerator
{
    private readonly Func<CompilationUnitSyntax, CompilationUnitSyntax> _rewrite;

    public RoslynRewriteViewGenerator(
        string viewKey,
        Func<CompilationUnitSyntax, CompilationUnitSyntax> rewrite)
    {
        ViewKey = viewKey;
        _rewrite = rewrite;
    }

    public string ViewKey { get; }

    public ExtractionResult<ViewGenerationResult> Generate(CompilationUnitSyntax root, ViewGeneratorContext context)
    {
        var rewritten = _rewrite(root);
        var text = rewritten.ToFullString();
        var hasSurface = CSharpCompilationUnitExportSurface.HasExportableSurface(rewritten);
        return ExtractionResult<ViewGenerationResult>.Success(new ViewGenerationResult(text, hasSurface));
    }
}
