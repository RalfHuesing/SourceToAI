using Microsoft.CodeAnalysis.CSharp;
using SourceToAI.CLI.Services.Processing;

namespace SourceToAI.Tests.Processing;

public class CSharpCompilationUnitExportSurfaceTests
{
    [Fact]
    public void HasExportableSurface_file_scoped_namespace_shell_only_is_false()
    {
        var root = SyntaxFactory.ParseCompilationUnit("namespace X;\r\n");
        Assert.False(CSharpCompilationUnitExportSurface.HasExportableSurface(root));
    }

    [Fact]
    public void HasExportableSurface_public_type_is_true()
    {
        var root = SyntaxFactory.ParseCompilationUnit("public class A { }");
        Assert.True(CSharpCompilationUnitExportSurface.HasExportableSurface(root));
    }

    [Fact]
    public void HasExportableSurface_global_statement_is_true()
    {
        var root = SyntaxFactory.ParseCompilationUnit("System.Console.WriteLine(1);");
        Assert.True(CSharpCompilationUnitExportSurface.HasExportableSurface(root));
    }
}
