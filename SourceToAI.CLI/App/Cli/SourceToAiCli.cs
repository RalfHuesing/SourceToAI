using System.CommandLine;

namespace SourceToAI.CLI.App.Cli;

internal readonly record struct CliParseValues(
    string? ExportPos,
    IReadOnlyList<string> SolutionPos,
    string? ExportOpt,
    IReadOnlyList<string> InputOpt,
    IReadOnlyList<string> GacPatterns);

/// <summary>
/// Zentral gebündelte Usage-Texte und Root-Command für System.CommandLine.
/// </summary>
internal static class SourceToAiCli
{
    internal const string RootDescription =
        "Exportiert eine oder mehrere .NET-Quellen (Solution-/Projektverzeichnis, kompilierte Assembly .dll/.exe oder GAC-Assemblys per --gac) als Multi-View-KI-Feed (Markdown) nacheinander in dasselbe Export-Verzeichnis. Optional: wiederholbare Option --exclude mit Glob-Mustern relativ zum jeweiligen Projektstamm.";

    internal const string UsageLine =
        "Verwendung: SourceToAI <Export-Pfad> <Verzeichnis|.dll|.exe>... | SourceToAI <Export-Pfad> --gac <Muster>... | SourceToAI --export <Export-Pfad> [--input <Pfad>...] [--gac <Muster>...] [--exclude <Glob>...]";

    internal const string UsageExamplePositional =
        "Beispiel (Positionsargumente): SourceToAI ./exports C:\\Daten\\RepoA\\ C:\\Daten\\RepoB\\";

    internal const string UsageExampleAssembly =
        "Beispiel (Assembly): SourceToAI ./exports C:\\Apps\\MyLib\\bin\\Debug\\net10.0\\MyLib.dll";

    internal const string UsageExampleAssemblyWildcard =
        "Beispiel (Assemblies mit Platzhalter * / ? im Dateinamen): SourceToAI ./exports C:\\Apps\\MyLib\\bin\\Debug\\net10.0\\*.dll";

    internal const string UsageExampleGac =
        "Beispiel (GAC): SourceToAI ./exports --gac \"Contoso.*.dll\" --gac \"Acme.Core.*.dll\"";

    internal const string UsageExampleOptions =
        "Beispiel (Optionen): SourceToAI --export ./exports --input C:\\Daten\\RepoA\\ --input C:\\Daten\\RepoB\\";

    internal const string UsageExampleExclude =
        "Beispiel (Ausschluss per Glob, relativ zum jeweiligen Projektordner): SourceToAI ./exports C:\\Repo\\ --exclude \"wwwroot/lib/**\" --exclude \"**/vis-timeline-graph2d.min.js\"";

    internal const string ExportPathDescription =
        "Zielverzeichnis für den gesamten Export-Baum (wird bei Bedarf angelegt; bestehender Inhalt wird sicherheitshalber geleert, sofern .sta-marker existiert).";

    internal const string SolutionRootDescription =
        "Quellverzeichnis (Solution/Repository mit .sln oder darüber) oder Pfad zu einer .NET-Assembly (.dll/.exe). Mehrere Pfade als weitere Positionsargumente; optional leer, wenn nur --gac genutzt wird.";

    internal const string ExportOptionDescription =
        "Export-Verzeichnis (Alternative zu Positionsargument <Export-Pfad>).";

    internal const string InputOptionDescription =
        "Quellverzeichnis oder Assembly (.dll/.exe); Alternative zu den weiteren Positionsargumenten. Mehrfach angebbare Option.";

    internal const string GacOptionDescription =
        "Dateinamen-Muster (*, ?) für DLLs im .NET-Framework-GAC; pro Assembly wird die höchste Version gewählt (MSIL vor GAC_32). Mehrfach angebbare Option. Mindestens ein --gac oder Quellpfad erforderlich.";

    internal const string ExcludeOptionDescription =
        "Glob-Muster für auszuschließende Dateien/Unterbäume (Microsoft.Extensions.FileSystemGlobbing). Relativ zum jeweiligen Projektstamm (Ordner der .csproj) und zusätzlich zur Solution-/Eingabe-Wurzel (z. B. ExternalTools für Ordner direkt unter der Wurzel). Mehrfach angebbbar. Unterbaum: wwwroot/lib/** oder Ordnername ohne Wildcards.";

    internal const string ErrorIncompletePositional =
        "Positionsargument <Export-Pfad> und mindestens ein Quellpfad (Verzeichnis, .dll/.exe) oder --gac sind erforderlich, wenn ohne --export/--input gearbeitet wird.";

    internal const string ErrorIncompleteNamed =
        "Option --export und mindestens ein --input oder --gac sind erforderlich, wenn ohne Positionsargumente gearbeitet wird.";

    internal const string ErrorPositionalAndNamed =
        "Positionsargumente und --export/--input duerfen nicht kombiniert werden.";

    internal const string ErrorNoInputOrGac =
        "Mindestens ein Quellpfad (--input / Positionsargument) oder mindestens ein --gac-Muster ist erforderlich.";

    private static readonly Argument<string?> ExportPositional = new("export-path")
    {
        Description = ExportPathDescription,
        Arity = ArgumentArity.ZeroOrOne,
    };

    private static readonly Argument<string[]> SolutionPositional = new("solution-root")
    {
        Description = SolutionRootDescription,
        Arity = ArgumentArity.ZeroOrMore,
    };

    private static readonly Option<string?> ExportOption = new("--export", [])
    {
        Description = ExportOptionDescription,
    };

    private static readonly Option<string[]> InputOption = new("--input", ["-i"])
    {
        Description = InputOptionDescription,
        Arity = ArgumentArity.OneOrMore,
    };

    private static readonly Option<string[]> GacOption = new("--gac")
    {
        Description = GacOptionDescription,
        Arity = ArgumentArity.OneOrMore,
    };

    private static readonly Option<string[]> ExcludeOption = new("--exclude")
    {
        Description = ExcludeOptionDescription,
        Arity = ArgumentArity.OneOrMore,
    };

    private static readonly Option<int> MaxFileSizeOption = new("--max-file-size")
    {
        Description = "Gewuenschte maximale Dateigroesse pro generierter Markdown-Datei in Kilobyte (Soft-Limit).",
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<int> MaxFileCountOption = new("--max-file-count")
    {
        Description = "Harte Obergrenze fuer die Anzahl der generierten Markdown-Dateien pro Projekt.",
        Arity = ArgumentArity.ExactlyOne,
    };

    private static readonly Option<bool> NoSuppressCoreOption = new("--no-suppress-core")
    {
        Description = "Legacy: C#-Dateien ohne Namespace als eigene Core-Partition (_Core) exportieren.",
    };

    /// <summary>
    /// Erzeugt den Root-Command; <paramref name="runAsync"/> wird bei gültiger CLI aufgerufen.
    /// </summary>
    internal static RootCommand CreateRootCommand(
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, IReadOnlyList<string>, int, int, bool, CancellationToken, Task<int>> runAsync)
    {
        var root = new RootCommand(RootDescription) { TreatUnmatchedTokensAsErrors = true };
        root.Add(ExportPositional);
        root.Add(SolutionPositional);
        root.Add(ExportOption);
        root.Add(InputOption);
        root.Add(GacOption);
        root.Add(ExcludeOption);
        root.Add(MaxFileSizeOption);
        root.Add(MaxFileCountOption);
        root.Add(NoSuppressCoreOption);

        root.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var values = ParseCliValues(parseResult);
            var resolution = ResolveInvocation(values);
            if (resolution.ExportPath is null
                || (resolution.SolutionPaths.Count == 0 && resolution.GacPatterns.Count == 0))
            {
                PrintPremiumHelp(resolution.ErrorMessage);
                return 1;
            }

            var excludeRaw = parseResult.GetValue(ExcludeOption);
            var excludePatterns = NormalizePathList(excludeRaw ?? Array.Empty<string>(), s => string.IsNullOrWhiteSpace(s) ? null : s.Trim());

            return await runAsync(
                resolution.ExportPath!,
                resolution.SolutionPaths,
                resolution.GacPatterns,
                excludePatterns,
                parseResult.GetValue(MaxFileSizeOption),
                parseResult.GetValue(MaxFileCountOption),
                parseResult.GetValue(NoSuppressCoreOption),
                cancellationToken);
        });

        return root;
    }

    private static CliParseValues ParseCliValues(ParseResult parseResult)
    {
        static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var exportPos = N(parseResult.GetValue(ExportPositional));
        var solutionPosRaw = parseResult.GetValue(SolutionPositional);
        var solutionPos = NormalizePathList(solutionPosRaw, N);
        var exportOpt = N(parseResult.GetValue(ExportOption));
        var inputOptRaw = parseResult.GetValue(InputOption);
        var inputOpt = NormalizePathList(inputOptRaw, N);
        var gacRaw = parseResult.GetValue(GacOption);
        var gacPatterns = NormalizePathList(gacRaw, N);

        return new CliParseValues(exportPos, solutionPos, exportOpt, inputOpt, gacPatterns);
    }

    private static CliPathResolution ResolveInvocation(CliParseValues values)
    {
        var isPos = values.ExportPos is not null || values.SolutionPos.Count > 0;
        var isNamed = values.ExportOpt is not null || values.InputOpt.Count > 0;

        if (isPos && isNamed)
            return CliPathResolution.Fail(ErrorPositionalAndNamed);

        if (isPos)
        {
            return ResolvePositional(values);
        }

        if (isNamed)
        {
            return ResolveNamed(values);
        }

        return CliPathResolution.Fail(null);
    }

    private static CliPathResolution ResolvePositional(CliParseValues values)
    {
        if (values.ExportPos is null || (values.SolutionPos.Count == 0 && values.GacPatterns.Count == 0))
            return CliPathResolution.Fail(ErrorIncompletePositional);
        return CliPathResolution.Ok(values.ExportPos, values.SolutionPos, values.GacPatterns);
    }

    private static CliPathResolution ResolveNamed(CliParseValues values)
    {
        if (values.ExportOpt is null || (values.InputOpt.Count == 0 && values.GacPatterns.Count == 0))
            return CliPathResolution.Fail(ErrorIncompleteNamed);
        return CliPathResolution.Ok(values.ExportOpt, values.InputOpt, values.GacPatterns);
    }

    private static IReadOnlyList<string> NormalizePathList(string[]? raw, Func<string?, string?> normalize)
    {
        if (raw is null || raw.Length == 0)
            return Array.Empty<string>();

        var list = new List<string>(raw.Length);
        foreach (var item in raw)
        {
            var n = normalize(item);
            if (n is not null)
                list.Add(n);
        }

        return list;
    }

    private readonly struct CliPathResolution
    {
        private CliPathResolution(
            string? exportPath,
            IReadOnlyList<string> solutionPaths,
            IReadOnlyList<string> gacPatterns,
            string? errorMessage)
        {
            ExportPath = exportPath;
            SolutionPaths = solutionPaths;
            GacPatterns = gacPatterns;
            ErrorMessage = errorMessage;
        }

        internal string? ExportPath { get; }
        internal IReadOnlyList<string> SolutionPaths { get; }
        internal IReadOnlyList<string> GacPatterns { get; }
        internal string? ErrorMessage { get; }

        internal static CliPathResolution Ok(
            string exportPath,
            IReadOnlyList<string> solutionPaths,
            IReadOnlyList<string> gacPatterns) =>
            new(exportPath, solutionPaths, gacPatterns, null);

        internal static CliPathResolution Fail(string? errorMessage) =>
            new(null, Array.Empty<string>(), Array.Empty<string>(), errorMessage);
    }

    internal static void PrintPremiumHelp(string? validationError = null)
    {
        if (validationError != null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[FEHLER] {validationError}");
            Console.ResetColor();
            Console.Error.WriteLine();
        }

        Console.WriteLine("==================================================================================");
        Console.WriteLine("SourceToAI - Standalone AI Feed Generator Help");
        Console.WriteLine("==================================================================================");
        Console.WriteLine();
        Console.WriteLine("SYNTAX (Eine der folgenden Varianten waehlen):");
        Console.WriteLine("  1) Positionsargumente:");
        Console.WriteLine("     SourceToAI <Export-Pfad> <Quell-Verzeichnis|.dll|.exe>... [Optionen]");
        Console.WriteLine();
        Console.WriteLine("  2) Benannte Optionen:");
        Console.WriteLine("     SourceToAI --export <Export-Pfad> --input <Quell-Verzeichnis|.dll|.exe>... [Optionen]");
        Console.WriteLine();
        Console.WriteLine("  3) Reiner GAC-Export (Decompilierung):");
        Console.WriteLine("     SourceToAI <Export-Pfad> --gac <Muster>... [Optionen]");
        Console.WriteLine();
        Console.WriteLine("OPTIONEN & ARGUMENTE:");
        Console.WriteLine("  <Export-Pfad> / --export      Zielverzeichnis fuer den gesamten Markdown-Export-Baum.");
        Console.WriteLine("  <Quelle> / --input            Quell-Repository, Solution-Ordner, oder Pfad zu einer .dll/.exe.");
        Console.WriteLine("  --gac <Muster>                Dateinamen-Muster (*, ?) fuer decompilierte Assemblys aus dem GAC.");
        Console.WriteLine("  --exclude <Glob>              Glob-Muster fuer auszuschliessende Dateien/Verzeichnisse.");
        Console.WriteLine("  --max-file-size <kb>          Gewuenschte maximale Dateigroesse pro Markdown-Datei (Soft-Limit).");
        Console.WriteLine("  --max-file-count <anzahl>     Harte Obergrenze fuer die Anzahl der Dateien pro Projekt.");
        Console.WriteLine("  --no-suppress-core            Legacy: eigene Core-Partition fuer C# ohne Namespace.");
        Console.WriteLine();
        Console.WriteLine("BEISPIELE:");
        Console.WriteLine("  - Einfacher Export:");
        Console.WriteLine("    SourceToAI C:\\AI_Feeds C:\\Daten\\MyRepo");
        Console.WriteLine();
        Console.WriteLine("  - Export mit intelligentem Namespace-Splitting (Clustering):");
        Console.WriteLine("    SourceToAI C:\\AI_Feeds C:\\Daten\\MyRepo --max-file-size 500 --max-file-count 8");
        Console.WriteLine();
        Console.WriteLine("  - Export mit Ausschluessen:");
        Console.WriteLine("    SourceToAI C:\\AI_Feeds C:\\Daten\\MyRepo --exclude \"wwwroot/lib/**\"");
        Console.WriteLine("==================================================================================");
    }
}
