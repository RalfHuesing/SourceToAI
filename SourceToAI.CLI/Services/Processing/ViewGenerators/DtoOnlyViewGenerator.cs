using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing.Rewriters;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

public sealed class DtoOnlyViewGenerator : IViewGenerator
{
    public string ViewKey => "dto-only";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context)
    {
        var rewritten = DtoRewriter.Rewrite(root);
        return ExtractionResult<string>.Success(rewritten.ToFullString());
    }
}
