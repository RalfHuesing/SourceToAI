using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceToAI.CLI.Services.Processing.Rewriters;

/// <summary>
/// Entfernt ausführbare Member-Bodies zugunsten einer Signatur-only-Darstellung
/// (<c>… { get; }</c>, Methoden/Operatoren/Konstruktoren mit <c>;</c>).
/// </summary>
public sealed class SignaturesRewriter : CSharpSyntaxRewriter
{
    private static readonly SyntaxToken SemicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);

    public static CompilationUnitSyntax Rewrite(CompilationUnitSyntax root) =>
        (CompilationUnitSyntax)new SignaturesRewriter().Visit(root)!;

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node) =>
        base.VisitMethodDeclaration(StripBaseMethodLike(node));

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node) =>
        base.VisitConstructorDeclaration(StripBaseMethodLike(node));

    public override SyntaxNode? VisitDestructorDeclaration(DestructorDeclarationSyntax node)
    {
        if (node.Body is null && node.ExpressionBody is null)
            return base.VisitDestructorDeclaration(node);

        var stripped = node
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SemicolonToken);
        return base.VisitDestructorDeclaration(stripped);
    }

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node) =>
        base.VisitOperatorDeclaration(StripBaseMethodLike(node));

    public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node) =>
        base.VisitConversionOperatorDeclaration(StripBaseMethodLike(node));

    public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
        if (node.Body is null && node.ExpressionBody is null)
            return base.VisitAccessorDeclaration(node);

        var stripped = node
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SemicolonToken);
        return base.VisitAccessorDeclaration(stripped);
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        PropertyDeclarationSyntax working = node;

        if (node.ExpressionBody is not null)
        {
            var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SemicolonToken);
            var list = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter));
            working = node
                .WithExpressionBody(null)
                .WithInitializer(null)
                .WithAccessorList(list)
                .WithSemicolonToken(default);
        }
        else if (node.Initializer is not null)
            working = node.WithInitializer(null).WithSemicolonToken(default);

        return base.VisitPropertyDeclaration(working);
    }

    public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        IndexerDeclarationSyntax working = node;

        if (node.ExpressionBody is not null)
        {
            var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SemicolonToken);
            var list = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(getter));
            working = node
                .WithExpressionBody(null)
                .WithAccessorList(list)
                .WithSemicolonToken(default);
        }

        return base.VisitIndexerDeclaration(working);
    }

    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        if (node.Body is null && node.ExpressionBody is null)
            return base.VisitLocalFunctionStatement(node);

        var stripped = node
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SemicolonToken);
        return base.VisitLocalFunctionStatement(stripped);
    }

    private static MethodDeclarationSyntax StripBaseMethodLike(MethodDeclarationSyntax node)
    {
        if (node.Body is null && node.ExpressionBody is null)
            return node;

        return node
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SemicolonToken);
    }

    private static ConstructorDeclarationSyntax StripBaseMethodLike(ConstructorDeclarationSyntax node)
    {
        if (node.Body is null && node.ExpressionBody is null)
            return node;

        return node
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SemicolonToken);
    }

    private static OperatorDeclarationSyntax StripBaseMethodLike(OperatorDeclarationSyntax node)
    {
        if (node.Body is null && node.ExpressionBody is null)
            return node;

        return node
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SemicolonToken);
    }

    private static ConversionOperatorDeclarationSyntax StripBaseMethodLike(ConversionOperatorDeclarationSyntax node)
    {
        if (node.Body is null && node.ExpressionBody is null)
            return node;

        return node
            .WithBody(null)
            .WithExpressionBody(null)
            .WithSemicolonToken(SemicolonToken);
    }
}
