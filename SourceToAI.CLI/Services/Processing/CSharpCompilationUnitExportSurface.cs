using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceToAI.CLI.Services.Processing;

/// <summary>
/// Ermittelt, ob eine Kompilationseinheit für den AI-Feed nach View-Rewrite noch exportierbare Oberfläche hat
/// (ohne erneutes Parsen von <see cref="Microsoft.CodeAnalysis.SyntaxNode.ToFullString"/>-Output).
/// </summary>
public static class CSharpCompilationUnitExportSurface
{
    /// <summary>
    /// Mindestens ein Typ/Enum/Delegate oder Top-Level-Statement — reine Namespace-/Using-Hülle liefert <c>false</c>.
    /// </summary>
    public static bool HasExportableSurface(CompilationUnitSyntax root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.DescendantNodes().Any(static n =>
            n is BaseTypeDeclarationSyntax
                or EnumDeclarationSyntax
                or DelegateDeclarationSyntax
                or GlobalStatementSyntax);
    }
}
