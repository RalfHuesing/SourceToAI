namespace SourceToAI.CLI.Services.Decompilation;

/// <summary>
/// Decompiliert eine .NET-Assembly in ein Projektverzeichnis mit <c>.csproj</c> (ILSpy-Engine).
/// </summary>
public interface IAssemblyDecompilerService
{
    /// <summary>
    /// Schreibt die Assembly nach <paramref name="targetDirectory"/> und gibt das Verzeichnis der Haupt-<c>.csproj</c> zurück.
    /// </summary>
    /// <param name="assemblyFilePath">Pfad zur <c>.dll</c> oder <c>.exe</c>.</param>
    /// <param name="targetDirectory">Zielbasis (wird bei Bedarf geleert und neu angelegt).</param>
    /// <param name="cancellationToken">Abbruchtoken für den Decompiler-Lauf.</param>
    string DecompileToProjectDirectory(
        string assemblyFilePath,
        string targetDirectory,
        CancellationToken cancellationToken = default);
}
