using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceToAI.CLI.Models;

namespace SourceToAI.CLI.Services.Processing.ViewGenerators;

/// <summary>Stub: später 1:1 Complete-View; aktuell Syntax-Rückgabe ohne Semantik.</summary>
public sealed class CompleteViewGenerator : IViewGenerator
{
    public string ViewKey => "complete";

    public ExtractionResult<string> Generate(CompilationUnitSyntax root, ViewGeneratorContext context) =>
        ExtractionResult<string>.Success(root.ToFullString());
}
