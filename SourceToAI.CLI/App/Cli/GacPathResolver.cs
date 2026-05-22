using SourceToAI.CLI.App.Exceptions;

namespace SourceToAI.CLI.App.Cli;

/// <summary>
/// Ermittelt den .NET-4+-GAC-Root (<c>%WINDIR%\Microsoft.NET\assembly</c>) oder einen konfigurierten Override.
/// </summary>
internal static class GacPathResolver
{
    private static readonly string[] KnownFlavorFolderNames = ["GAC_MSIL", "GAC_32", "GAC_64"];

    /// <summary>
    /// Vollständiger Pfad zum <c>assembly</c>-Ordner (enthält <c>GAC_MSIL</c> usw.).
    /// </summary>
    internal static string ResolveRoot(string? configuredAssemblyRoot)
    {
        if (!string.IsNullOrWhiteSpace(configuredAssemblyRoot))
        {
            var trimmed = configuredAssemblyRoot.Trim();
            if (!Directory.Exists(trimmed))
            {
                throw new SourceToAiValidationException(
                    $"Konfigurierter GAC-Assembly-Root existiert nicht: \"{trimmed}\".");
            }

            return Path.GetFullPath(trimmed);
        }

        var fromWindows = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            "Microsoft.NET",
            "assembly");
        if (Directory.Exists(fromWindows))
            return Path.GetFullPath(fromWindows);

        var windir = Environment.GetEnvironmentVariable("WINDIR");
        if (!string.IsNullOrWhiteSpace(windir))
        {
            var fromWindir = Path.Combine(windir.Trim(), "Microsoft.NET", "assembly");
            if (Directory.Exists(fromWindir))
                return Path.GetFullPath(fromWindir);
        }

        throw new SourceToAiValidationException(
            "Kein .NET-Framework-GAC gefunden (erwartet unter %WINDIR%\\Microsoft.NET\\assembly). " +
            "Unter Windows mit installiertem .NET Framework ausführen oder SourceToAI:GacAssemblyRoot setzen.");
    }

    /// <summary>
    /// Existierende Flavor-Unterordner unter dem Assembly-Root (nur <c>GAC_MSIL</c>, <c>GAC_32</c>, <c>GAC_64</c>).
    /// </summary>
    internal static IReadOnlyList<(string FlavorLabel, string FullPath, int Rank)> ResolveFlavorRoots(string assemblyRoot)
    {
        var list = new List<(string, string, int)>();
        for (var i = 0; i < KnownFlavorFolderNames.Length; i++)
        {
            var name = KnownFlavorFolderNames[i];
            var path = Path.Combine(assemblyRoot, name);
            if (Directory.Exists(path))
                list.Add((FlavorLabelFromFolder(name), path, i));
        }

        return list;
    }

    private static string FlavorLabelFromFolder(string folderName) =>
        folderName switch
        {
            "GAC_MSIL" => "MSIL",
            "GAC_32" => "32",
            "GAC_64" => "64",
            _ => folderName,
        };
}
