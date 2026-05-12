using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

/// <summary>Complete-View: bevorzugt Originaltext aus dem Loader, sonst <c>ToFullString()</c>.</summary>
public sealed class CompleteViewGenerator : IViewGenerator
{
    public string ViewKey => "complete";

    public ExtractionResult<ViewGenerationResult> Generate(CompilationUnitSyntax root, ViewGeneratorContext context)
    {
        if (context.OriginalSourceText is not null)
            return ExtractionResult<ViewGenerationResult>.Success(
                new ViewGenerationResult(context.OriginalSourceText, HasExportableSurface: true));

        var text = root.ToFullString();
        return ExtractionResult<ViewGenerationResult>.Success(new ViewGenerationResult(text, HasExportableSurface: true));
    }
}
