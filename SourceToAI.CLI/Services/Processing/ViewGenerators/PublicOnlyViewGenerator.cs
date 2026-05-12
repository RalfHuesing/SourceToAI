using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

/// <summary>Stub für Task 04 (<c>VisibilityRewriter</c>).</summary>
public sealed class PublicOnlyViewGenerator : IViewGenerator
{
    public string ViewKey => "public-only";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context) =>
        ExtractionResult<string>.Success(root.ToFullString());
}
