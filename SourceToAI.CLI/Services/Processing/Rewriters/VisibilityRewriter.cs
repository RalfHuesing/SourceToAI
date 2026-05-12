using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceToAI.CLI.Services.Processing.Rewriters;

/// <summary>
/// Entfernt Member und Typen, die nicht zur öffentlichen bzw. geschützten API gehören
/// (<c>private</c>/<c>internal</c> in Modifierlisten, implizit private Typen/Member,
/// Top-Level-Typen ohne <c>public</c>).
/// </summary>
public sealed class VisibilityRewriter : CSharpSyntaxRewriter
{
    public static CompilationUnitSyntax Rewrite(CompilationUnitSyntax root) =>
        (CompilationUnitSyntax)new VisibilityRewriter().Visit(root)!;

    public override SyntaxNode? VisitGlobalStatement(GlobalStatementSyntax node)
    {
        var stmt = Visit(node.Statement);
        if (stmt is null)
            return null;
        return node.WithStatement((StatementSyntax)stmt);
    }

    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) =>
        node.Parent is GlobalStatementSyntax ? null : base.VisitLocalFunctionStatement(node);

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node) =>
        VisitTypeLikeDeclaration(node, base.VisitClassDeclaration);

    public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node) =>
        VisitTypeLikeDeclaration(node, base.VisitStructDeclaration);

    public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) =>
        VisitTypeLikeDeclaration(node, base.VisitInterfaceDeclaration);

    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node) =>
        VisitTypeLikeDeclaration(node, base.VisitRecordDeclaration);

    public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        if (ShouldRemoveAggregateType(node.Modifiers, IsTopLevelTypeDeclaration(node)))
            return null;
        return base.VisitEnumDeclaration(node);
    }

    public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node)
    {
        if (ShouldRemoveAggregateType(node.Modifiers, IsTopLevelTypeDeclaration(node)))
            return null;
        return base.VisitDelegateDeclaration(node);
    }

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (node.ExplicitInterfaceSpecifier is not null)
            return base.VisitMethodDeclaration(node);
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitMethodDeclaration(node);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitConstructorDeclaration(node);
    }

    public override SyntaxNode? VisitDestructorDeclaration(DestructorDeclarationSyntax node) =>
        base.VisitDestructorDeclaration(node);

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        if (node.ExplicitInterfaceSpecifier is not null)
            return base.VisitPropertyDeclaration(node);
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitPropertyDeclaration(node);
    }

    public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        if (node.ExplicitInterfaceSpecifier is not null)
            return base.VisitIndexerDeclaration(node);
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitIndexerDeclaration(node);
    }

    public override SyntaxNode? VisitEventDeclaration(EventDeclarationSyntax node)
    {
        if (node.ExplicitInterfaceSpecifier is not null)
            return base.VisitEventDeclaration(node);
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitEventDeclaration(node);
    }

    public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitEventFieldDeclaration(node);
    }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitFieldDeclaration(node);
    }

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitOperatorDeclaration(node);
    }

    public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        if (ShouldRemoveMemberByAccess(node, node.Modifiers))
            return null;
        return base.VisitConversionOperatorDeclaration(node);
    }

    private SyntaxNode? VisitTypeLikeDeclaration<TNode>(TNode node, Func<TNode, SyntaxNode?> recurse)
        where TNode : TypeDeclarationSyntax
    {
        if (ShouldRemoveAggregateType(node.Modifiers, IsTopLevelTypeDeclaration(node)))
            return null;
        return recurse(node);
    }

    private static bool IsTopLevelTypeDeclaration(SyntaxNode node) =>
        node.Parent is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax;

    private static bool ShouldRemoveAggregateType(SyntaxTokenList modifiers, bool topLevel)
    {
        if (topLevel)
            return !modifiers.Any(static m => m.IsKind(SyntaxKind.PublicKeyword));

        if (modifiers.Any(static m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.InternalKeyword)))
            return true;

        return !modifiers.Any(static m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword));
    }

    private static bool ShouldRemoveMemberByAccess(SyntaxNode node, SyntaxTokenList modifiers)
    {
        if (node.Parent is InterfaceDeclarationSyntax)
        {
            return modifiers.Any(static m =>
                m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.InternalKeyword));
        }

        if (modifiers.Any(static m => m.IsKind(SyntaxKind.PrivateKeyword) || m.IsKind(SyntaxKind.InternalKeyword)))
            return true;

        return !modifiers.Any(static m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword));
    }
}
