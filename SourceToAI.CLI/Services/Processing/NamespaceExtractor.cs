using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Statische Hilfsklasse zur performanten Extraktion von Namespaces aus Roslyn-Syntaxbäumen.
/// </summary>
public static class NamespaceExtractor
{
    /// <summary>
    /// Ermittelt den Namespace aus einem C#-Kompilierungsdokument (Roslyn-AST).
    /// Berücksichtigt sowohl klassische geschweifte Namespaces als auch File-Scoped-Namespaces (C# 10+).
    /// </summary>
    public static string GetNamespace(CompilationUnitSyntax root)
    {
        if (root == null)
            return string.Empty;

        var namespaceDecl = root.Members.OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDecl?.Name.ToString()?.Trim() ?? string.Empty;
    }
}
