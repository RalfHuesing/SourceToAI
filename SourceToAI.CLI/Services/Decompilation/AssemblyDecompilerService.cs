using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using SourceToAI.CLI.App.Exceptions;
using static ICSharpCode.Decompiler.Metadata.MetadataExtensions;

namespace SourceToAI.CLI.Services.Decompilation;

public sealed class AssemblyDecompilerService : IAssemblyDecompilerService
{
    public string DecompileToProjectDirectory(
        string assemblyFilePath,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(assemblyFilePath))
            throw new SourceToAiValidationException("Der Pfad zur Assembly darf nicht leer sein.");

        var assemblyFullPath = Path.GetFullPath(assemblyFilePath);
        if (!File.Exists(assemblyFullPath))
            throw new SourceToAiValidationException($"Die Assembly-Datei wurde nicht gefunden: {assemblyFullPath}");

        var ext = Path.GetExtension(assemblyFullPath);
        if (!ext.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            && !ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
            throw new SourceToAiValidationException(
                "Es werden nur kompilierte .NET-Assemblies (.dll oder .exe) unterstützt.");

        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new SourceToAiValidationException("Das Zielverzeichnis darf nicht leer sein.");

        var targetFullPath = Path.GetFullPath(targetDirectory);
        PrepareEmptyTargetDirectory(targetFullPath);

        using var module = new PEFile(assemblyFullPath);

        var targetFrameworkId = module.DetectTargetFrameworkId();
        var resolver = new UniversalAssemblyResolver(
            assemblyFullPath,
            throwOnError: false,
            targetFrameworkId);

        var assemblyDir = Path.GetDirectoryName(assemblyFullPath);
        if (!string.IsNullOrEmpty(assemblyDir))
            resolver.AddSearchDirectory(assemblyDir);

        var settings = new DecompilerSettings
        {
            RemoveDeadCode = true,
            YieldReturn = true,
            AsyncAwait = true,
        };

        var decompiler = new WholeProjectDecompiler(
            settings,
            resolver,
            projectWriter: null,
            assemblyReferenceClassifier: null,
            debugInfoProvider: null);

        decompiler.DecompileProject(module, targetFullPath, cancellationToken);

        return ResolveProjectDirectoryAfterDecompile(targetFullPath, module.Name);
    }

    static void PrepareEmptyTargetDirectory(string targetFullPath)
    {
        if (Directory.Exists(targetFullPath))
            Directory.Delete(targetFullPath, recursive: true);

        Directory.CreateDirectory(targetFullPath);
    }

    static string ResolveProjectDirectoryAfterDecompile(string targetFullPath, string assemblySimpleName)
    {
        var csprojs = Directory.GetFiles(targetFullPath, "*.csproj", SearchOption.AllDirectories);
        if (csprojs.Length == 0)
            throw new SourceToAiValidationException(
                "Nach der Decompilierung wurde keine .csproj-Datei im Zielverzeichnis gefunden. "
                + "Prüfen Sie die Assembly und die Abhängigkeiten.");

        string? primary = csprojs.FirstOrDefault(p =>
            string.Equals(Path.GetFileNameWithoutExtension(p), assemblySimpleName, StringComparison.OrdinalIgnoreCase));

        primary ??= csprojs[0];
        return Path.GetDirectoryName(Path.GetFullPath(primary))!;
    }
}
