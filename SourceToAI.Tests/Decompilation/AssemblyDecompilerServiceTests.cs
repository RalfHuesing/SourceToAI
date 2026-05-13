using SourceToAI.CLI.App.Exceptions;
using SourceToAI.CLI.Services.Decompilation;
using SourceToAI.Tests.Support;

namespace SourceToAI.Tests.Decompilation;

public sealed class AssemblyDecompilerServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "SourceToAI.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // best effort
        }
    }

    [Fact]
    public void DecompileToProjectDirectory_emits_csproj_and_cs_for_minimal_assembly()
    {
        var assemblyName = "StDecompEmit" + Guid.NewGuid().ToString("N")[..8];
        var buildDir = Path.Combine(_tempRoot, "build");
        var dllPath = MinimalTestAssemblyCompiler.EmitClassLibraryDll(buildDir, assemblyName);

        var decompileRoot = Path.Combine(_tempRoot, "decompile-out");
        var sut = new AssemblyDecompilerService();

        var projectDir = sut.DecompileToProjectDirectory(dllPath, decompileRoot, TestContext.Current.CancellationToken);

        Assert.True(Directory.Exists(projectDir), projectDir);
        var csprojs = Directory.GetFiles(projectDir, "*.csproj", SearchOption.AllDirectories);
        Assert.NotEmpty(csprojs);
        var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(csFiles);
    }

    [Fact]
    public void DecompileToProjectDirectory_rejects_non_dll_extension()
    {
        var badPath = Path.Combine(_tempRoot, "note.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(badPath)!);
        File.WriteAllText(badPath, "x");

        var sut = new AssemblyDecompilerService();
        var ex = Assert.Throws<SourceToAiValidationException>(() =>
            sut.DecompileToProjectDirectory(badPath, Path.Combine(_tempRoot, "out"), TestContext.Current.CancellationToken));

        Assert.Contains(".dll", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecompileToProjectDirectory_rejects_missing_file()
    {
        var missing = Path.Combine(_tempRoot, "gone", "missing.dll");
        var sut = new AssemblyDecompilerService();
        var ex = Assert.Throws<SourceToAiValidationException>(() =>
            sut.DecompileToProjectDirectory(missing, Path.Combine(_tempRoot, "out"), TestContext.Current.CancellationToken));

        Assert.Contains("nicht gefunden", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
