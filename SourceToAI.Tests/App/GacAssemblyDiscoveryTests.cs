using SourceToAI.CLI.App.Cli;
using SourceToAI.CLI.App.Exceptions;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.App;

public sealed class GacAssemblyDiscoveryTests
{
    [Fact]
    public void Resolve_TwoVersions_PicksHighest()
    {
        using var ws = new TempWorkspace();
        var root = CreateAssemblyRoot(ws);
        var token = "0123456789abcdef";
        WriteDll(root, "GAC_MSIL", "Contoso.Lib", "v4.0_8.0.0.0__" + token, "Contoso.Lib.dll");
        var expected = WriteDll(root, "GAC_MSIL", "Contoso.Lib", "v4.0_9.0.0.0__" + token, "Contoso.Lib.dll");

        var result = GacAssemblyDiscovery.Resolve(["Contoso.Lib.dll"], root);

        Assert.Single(result);
        Assert.Equal(expected, result[0].FullPath);
        Assert.Equal(new Version(9, 0, 0, 0), result[0].Version);
        Assert.Equal("MSIL", result[0].FlavorLabel);
    }

    [Fact]
    public void Resolve_TwoPatterns_UnionOfMatches()
    {
        using var ws = new TempWorkspace();
        var root = CreateAssemblyRoot(ws);
        var token = "0123456789abcdef";
        var a = WriteDll(root, "GAC_MSIL", "Contoso.A", "v4.0_1.0.0.0__" + token, "Contoso.A.dll");
        var b = WriteDll(root, "GAC_MSIL", "Contoso.B", "v4.0_1.0.0.0__" + token, "Contoso.B.dll");

        var result = GacAssemblyDiscovery.Resolve(["Contoso.A.dll", "Contoso.B.dll"], root);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => string.Equals(r.FullPath, a, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, r => string.Equals(r.FullPath, b, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Resolve_PatternWithoutMatch_Throws()
    {
        using var ws = new TempWorkspace();
        var root = CreateAssemblyRoot(ws);
        var token = "0123456789abcdef";
        WriteDll(root, "GAC_MSIL", "Contoso.Lib", "v4.0_1.0.0.0__" + token, "Contoso.Lib.dll");

        var ex = Assert.Throws<SourceToAiValidationException>(() =>
            GacAssemblyDiscovery.Resolve(["Missing.*.dll"], root));

        Assert.Contains("Keine GAC-Treffer", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Missing.*.dll", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_MsilAnd32SameIdentity_KeepsMsil()
    {
        using var ws = new TempWorkspace();
        var root = CreateAssemblyRoot(ws);
        var token = "0123456789abcdef";
        var msil = WriteDll(root, "GAC_MSIL", "Contoso.Lib", "v4.0_9.0.0.0__" + token, "Contoso.Lib.dll");
        WriteDll(root, "GAC_32", "Contoso.Lib", "v4.0_9.0.0.0__" + token, "Contoso.Lib.dll");

        var result = GacAssemblyDiscovery.Resolve(["Contoso.Lib.dll"], root);

        Assert.Single(result);
        Assert.Equal(msil, result[0].FullPath, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("MSIL", result[0].FlavorLabel);
    }

    [Fact]
    public void Resolve_WildcardPattern_MatchesMultipleDlls()
    {
        using var ws = new TempWorkspace();
        var root = CreateAssemblyRoot(ws);
        var token = "0123456789abcdef";
        var a = WriteDll(root, "GAC_MSIL", "Contoso.A", "v4.0_1.0.0.0__" + token, "Contoso.A.dll");
        var b = WriteDll(root, "GAC_MSIL", "Contoso.B", "v4.0_1.0.0.0__" + token, "Contoso.B.dll");

        var result = GacAssemblyDiscovery.Resolve(["Contoso.*.dll"], root);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => string.Equals(r.FullPath, a, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result, r => string.Equals(r.FullPath, b, StringComparison.OrdinalIgnoreCase));
    }

    private static string CreateAssemblyRoot(TempWorkspace ws)
    {
        var root = Path.Combine(ws.Root, "assembly");
        Directory.CreateDirectory(Path.Combine(root, "GAC_MSIL"));
        return root;
    }

    private static string WriteDll(
        string assemblyRoot,
        string flavor,
        string assemblyName,
        string versionFolder,
        string fileName)
    {
        var dir = Path.Combine(assemblyRoot, flavor, assemblyName, versionFolder);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(dir, fileName);
        File.WriteAllText(fullPath, "dll");
        return fullPath;
    }
}
