using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

/// <summary>Complete-View: bevorzugt Originaltext aus dem Loader, sonst <c>ToFullString()</c>.</summary>
public sealed class CompleteViewGenerator : IViewGenerator
{
    public string ViewKey => "complete";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context)
    {
        if (context.OriginalSourceText is not null)
            return ExtractionResult<string>.Success(context.OriginalSourceText);

        return ExtractionResult<string>.Success(root.ToFullString());
    }
}
