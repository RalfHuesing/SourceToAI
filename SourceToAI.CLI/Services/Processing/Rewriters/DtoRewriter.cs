using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceToAI.CLI.Services.Processing.Rewriters;

/// <summary>Roslyn-Rewriter für die <c>dto-only</c>-Sicht (Typen filtern).</summary>
public sealed class DtoRewriter : CSharpSyntaxRewriter
{
    private bool _visitChildrenWithoutMemberFilter;

    /// <summary>
    /// DTO-Heuristik: <c>enum</c> und jeder <c>record</c>-Typ bleiben; andere Klassen/Structs nur ohne Primary-Constructor-Parameter und mit ausschließlich Feldern, reinen Auto-Properties sowie leeren Instanzkonstruktoren, wobei ein verbotener Direkt-Member (z. B. jede Methode) den gesamten Typ verwirft und ungeeignete verschachtelte Typen einzeln entfallen.
    /// </summary>
    public static CompilationUnitSyntax Rewrite(CompilationUnitSyntax root) =>
        (CompilationUnitSyntax)new DtoRewriter().Visit(root)!;

    public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
    {
        var visited = (CompilationUnitSyntax?)base.VisitCompilationUnit(node);
        if (visited is null)
            return null;

        return visited.WithMembers(StripEmptyNamespaceMembers(visited.Members));
    }

    public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        var visited = (NamespaceDeclarationSyntax?)base.VisitNamespaceDeclaration(node);
        if (visited is null)
            return null;

        var members = StripEmptyNamespaceMembers(visited.Members);
        return members.Any() ? visited.WithMembers(members) : null;
    }

    public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        var visited = (FileScopedNamespaceDeclarationSyntax?)base.VisitFileScopedNamespaceDeclaration(node);
        if (visited is null)
            return null;

        var members = StripEmptyNamespaceMembers(visited.Members);
        return members.Any() ? visited.WithMembers(members) : null;
    }

    public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => null;

    public override SyntaxNode? VisitDelegateDeclaration(DelegateDeclarationSyntax node) => null;

    public override SyntaxNode? VisitGlobalStatement(GlobalStatementSyntax node) => null;

    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        _visitChildrenWithoutMemberFilter = true;
        try
        {
            return base.VisitRecordDeclaration(node);
        }
        finally
        {
            _visitChildrenWithoutMemberFilter = false;
        }
    }

    public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node) =>
        base.VisitEnumDeclaration(node);

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (_visitChildrenWithoutMemberFilter)
            return base.VisitClassDeclaration(node);

        if (HasPrimaryConstructorParameters(node))
            return null;

        if (HasBlockingDirectMember(node))
            return null;

        var filteredMembers = FilterDataClassMembers(node.Members);
        if (!filteredMembers.Any())
            return null;

        var withMembers = node.WithMembers(SyntaxFactory.List(filteredMembers));

        _visitChildrenWithoutMemberFilter = true;
        try
        {
            return base.VisitClassDeclaration(withMembers);
        }
        finally
        {
            _visitChildrenWithoutMemberFilter = false;
        }
    }

    public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (_visitChildrenWithoutMemberFilter)
            return base.VisitStructDeclaration(node);

        if (HasPrimaryConstructorParameters(node))
            return null;

        if (HasBlockingDirectMember(node))
            return null;

        var filteredMembers = FilterDataClassMembers(node.Members);
        if (!filteredMembers.Any())
            return null;

        var withMembers = node.WithMembers(SyntaxFactory.List(filteredMembers));

        _visitChildrenWithoutMemberFilter = true;
        try
        {
            return base.VisitStructDeclaration(withMembers);
        }
        finally
        {
            _visitChildrenWithoutMemberFilter = false;
        }
    }

    private static SyntaxList<MemberDeclarationSyntax> StripEmptyNamespaceMembers(
        SyntaxList<MemberDeclarationSyntax> members)
    {
        return SyntaxFactory.List(members.Where(m => m is not BaseNamespaceDeclarationSyntax ns || HasAnyTypeMember(ns)));
    }

    private static bool HasAnyTypeMember(BaseNamespaceDeclarationSyntax ns)
    {
        foreach (var m in ns.Members)
        {
            if (m is BaseNamespaceDeclarationSyntax nested)
            {
                if (HasAnyTypeMember(nested))
                    return true;
            }
            else
                return true;
        }

        return false;
    }

    private IEnumerable<MemberDeclarationSyntax> FilterDataClassMembers(SyntaxList<MemberDeclarationSyntax> members)
    {
        foreach (var m in members)
        {
            if (GetFilteredMember(m) is { } filtered)
                yield return filtered;
        }
    }

    private MemberDeclarationSyntax? GetFilteredMember(MemberDeclarationSyntax m)
    {
        return m switch
        {
            FieldDeclarationSyntax or EnumDeclarationSyntax => Visit(m) as MemberDeclarationSyntax,
            PropertyDeclarationSyntax p when IsAutoStyleProperty(p) => Visit(p) as MemberDeclarationSyntax,
            ConstructorDeclarationSyntax c when !c.Modifiers.Any(SyntaxKind.StaticKeyword) && IsAllowedEmptyInstanceCtor(c) => Visit(c) as MemberDeclarationSyntax,
            ClassDeclarationSyntax cls => VisitClassDeclaration(cls) as MemberDeclarationSyntax,
            StructDeclarationSyntax st => VisitStructDeclaration(st) as MemberDeclarationSyntax,
            RecordDeclarationSyntax rec => VisitRecordDeclaration(rec) as MemberDeclarationSyntax,
            _ => null
        };
    }

    private static bool HasPrimaryConstructorParameters(TypeDeclarationSyntax node) =>
        node.ParameterList is not null && node.ParameterList.Parameters.Count > 0;

    private static bool HasBlockingDirectMember(TypeDeclarationSyntax node) =>
        node.Members.Any(IsBlockingMember);

    private static bool IsBlockingMember(MemberDeclarationSyntax m)
    {
        return m switch
        {
            MethodDeclarationSyntax or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax 
                or DestructorDeclarationSyntax or IndexerDeclarationSyntax or EventDeclarationSyntax 
                or EventFieldDeclarationSyntax => true,

            PropertyDeclarationSyntax p when !IsAutoStyleProperty(p) => true,

            ConstructorDeclarationSyntax c => c.Modifiers.Any(SyntaxKind.StaticKeyword) || !IsAllowedEmptyInstanceCtor(c),

            _ => false
        };
    }

    private static bool IsAllowedEmptyInstanceCtor(ConstructorDeclarationSyntax c)
    {
        if (c.Modifiers.Any(SyntaxKind.StaticKeyword))
            return false;

        if (c.ExpressionBody is not null)
            return false;

        if (c.Body is null)
            return false;

        return !c.Body.Statements.Any();
    }

    private static bool IsAutoStyleProperty(PropertyDeclarationSyntax p)
    {
        if (p.ExpressionBody is not null)
            return false;

        if (p.AccessorList is null)
            return false;

        foreach (var accessor in p.AccessorList.Accessors)
        {
            if (accessor.Body is not null || accessor.ExpressionBody is not null)
                return false;
        }

        return p.AccessorList.Accessors.Count > 0;
    }
}
