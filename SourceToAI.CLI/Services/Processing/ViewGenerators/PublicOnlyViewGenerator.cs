using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing.Rewriters;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

public sealed class PublicOnlyViewGenerator : IViewGenerator
{
    public string ViewKey => "public-only";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context)
    {
        var rewritten = VisibilityRewriter.Rewrite(root);
        return ExtractionResult<string>.Success(rewritten.ToFullString());
    }
}
