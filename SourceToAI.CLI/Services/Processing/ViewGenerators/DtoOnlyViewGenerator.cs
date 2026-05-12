using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

/// <summary>Stub für Task 05 (DTO-Filter).</summary>
public sealed class DtoOnlyViewGenerator : IViewGenerator
{
    public string ViewKey => "dto-only";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context) =>
        ExtractionResult<string>.Success(root.ToFullString());
}
