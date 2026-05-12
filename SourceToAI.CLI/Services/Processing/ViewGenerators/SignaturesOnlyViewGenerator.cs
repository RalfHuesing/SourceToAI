using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

/// <summary>Stub für Task 03 (<c>SignaturesRewriter</c>).</summary>
public sealed class SignaturesOnlyViewGenerator : IViewGenerator
{
    public string ViewKey => "signatures-only";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context) =>
        ExtractionResult<string>.Success(root.ToFullString());
}
