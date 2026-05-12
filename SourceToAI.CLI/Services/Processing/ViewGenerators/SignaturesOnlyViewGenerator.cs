using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing;
using SourceToAI.CLI.Services.Processing.Rewriters;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

public sealed class SignaturesOnlyViewGenerator : IViewGenerator
{
    public string ViewKey => "signatures-only";

    public ExtractionResult<ViewGenerationResult> Generate(CompilationUnitSyntax root, ViewGeneratorContext context)
    {
        var rewritten = SignaturesRewriter.Rewrite(root);
        var text = rewritten.ToFullString();
        var hasSurface = CSharpCompilationUnitExportSurface.HasExportableSurface(rewritten);
        return ExtractionResult<ViewGenerationResult>.Success(new ViewGenerationResult(text, hasSurface));
    }
}
