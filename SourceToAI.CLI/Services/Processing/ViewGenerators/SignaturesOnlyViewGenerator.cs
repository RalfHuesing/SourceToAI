using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;
using SourceToAI.CLI.Services.Processing.Rewriters;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

public sealed class SignaturesOnlyViewGenerator : IViewGenerator
{
    public string ViewKey => "signatures-only";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context)
    {
        var rewritten = SignaturesRewriter.Rewrite(root);
        return ExtractionResult<string>.Success(rewritten.ToFullString());
    }
}
