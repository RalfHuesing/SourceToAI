using System.CommandLine;

namespace SourceToAI.CLI.App.Cli;

/// <summary>
/// Zentral gebündelte Usage-Texte und Root-Command für System.CommandLine.
/// </summary>
internal static class SourceToAiCli
{
    internal static class Usage
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
    }

    /// <summary>
    /// Erzeugt den Root-Command; <paramref name="runAsync"/> wird bei gültiger CLI aufgerufen.
    /// </summary>
    internal static RootCommand CreateRootCommand(
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, IReadOnlyList<string>, int, int, bool, CancellationToken, Task<int>> runAsync)
    {
        var exportPositional = new Argument<string?>("export-path")
        {
            Description = Usage.ExportPathDescription,
            Arity = ArgumentArity.ZeroOrOne,
        };
        var solutionPositional = new Argument<string[]>("solution-root")
        {
            Description = Usage.SolutionRootDescription,
            // ZeroOrMore: sonst würden im reinen Optionsmodus (--export/--input) künstlich Positionsargumente erzwungen.
            Arity = ArgumentArity.ZeroOrMore,
        };

        var exportOption = new Option<string?>("--export", [])
        {
            Description = Usage.ExportOptionDescription,
        };
        var inputOption = new Option<string[]>("--input", ["-i"])
        {
            Description = Usage.InputOptionDescription,
            Arity = ArgumentArity.OneOrMore,
        };
        var gacOption = new Option<string[]>("--gac")
        {
            Description = Usage.GacOptionDescription,
            Arity = ArgumentArity.OneOrMore,
        };
        var excludeOption = new Option<string[]>("--exclude")
        {
            Description = Usage.ExcludeOptionDescription,
            Arity = ArgumentArity.OneOrMore,
        };
        var maxFileSizeOption = new Option<int>("--max-file-size")
        {
            Description = "Gewuenschte maximale Dateigroesse pro generierter Markdown-Datei in Kilobyte (Soft-Limit).",
            Arity = ArgumentArity.ExactlyOne,
        };
        var maxFileCountOption = new Option<int>("--max-file-count")
        {
            Description = "Harte Obergrenze fuer die Anzahl der generierten Markdown-Dateien pro Projekt.",
            Arity = ArgumentArity.ExactlyOne,
        };
        var noSuppressCoreOption = new Option<bool>("--no-suppress-core")
        {
            Description = "Legacy: C#-Dateien ohne Namespace als eigene Core-Partition (_Core) exportieren.",
        };

        var root = new RootCommand(Usage.RootDescription)
        {
            TreatUnmatchedTokensAsErrors = true,
        };
        root.Add(exportPositional);
        root.Add(solutionPositional);
        root.Add(exportOption);
        root.Add(inputOption);
        root.Add(gacOption);
        root.Add(excludeOption);
        root.Add(maxFileSizeOption);
        root.Add(maxFileCountOption);
        root.Add(noSuppressCoreOption);

        root.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var resolution = ResolveInvocation(
                parseResult,
                exportPositional,
                solutionPositional,
                inputOption,
                exportOption,
                gacOption);
            if (resolution.ExportPath is null
                || (resolution.SolutionPaths.Count == 0 && resolution.GacPatterns.Count == 0))
            {
                PrintPremiumHelp(resolution.ErrorMessage);
                return 1;
            }

            string? TrimTok(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            var excludeRaw = parseResult.GetValue(excludeOption);
            var excludePatterns = NormalizePathList(excludeRaw ?? Array.Empty<string>(), TrimTok);
            var maxFileSize = parseResult.GetValue(maxFileSizeOption);
            var maxFileCount = parseResult.GetValue(maxFileCountOption);
            var noSuppressCore = parseResult.GetValue(noSuppressCoreOption);

            return await runAsync(
                resolution.ExportPath!,
                resolution.SolutionPaths,
                resolution.GacPatterns,
                excludePatterns,
                maxFileSize,
                maxFileCount,
                noSuppressCore,
                cancellationToken);
        });

        return root;
    }

    private static CliPathResolution ResolveInvocation(
        ParseResult parseResult,
        Argument<string?> exportPositional,
        Argument<string[]> solutionPositional,
        Option<string[]> inputOption,
        Option<string?> exportOption,
        Option<string[]> gacOption)
    {
        static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var exportPos = N(parseResult.GetValue(exportPositional));
        var solutionPosRaw = parseResult.GetValue(solutionPositional);
        var solutionPos = NormalizePathList(solutionPosRaw, N);
        var exportOpt = N(parseResult.GetValue(exportOption));
        var inputOptRaw = parseResult.GetValue(inputOption);
        var inputOpt = NormalizePathList(inputOptRaw, N);
        var gacRaw = parseResult.GetValue(gacOption);
        var gacPatterns = NormalizePathList(gacRaw, N);

        var hasExportPos = exportPos is not null;
        var hasSolutionPos = solutionPos.Count > 0;
        var hasGac = gacPatterns.Count > 0;
        var hasPositionalExportWithSource = hasExportPos && (hasSolutionPos || hasGac);
        var hasPartialPositional = (hasExportPos && !hasSolutionPos && !hasGac)
                                   || (!hasExportPos && hasSolutionPos);

        var hasExportOpt = exportOpt is not null;
        var hasInputOpt = inputOpt.Count > 0;
        var hasNamedExportWithSource = hasExportOpt && (hasInputOpt || hasGac);
        var hasPartialNamed = (hasExportOpt && !hasInputOpt && !hasGac)
                              || (!hasExportOpt && hasInputOpt);

        var hasPositional = hasPositionalExportWithSource;
        var hasNamed = hasNamedExportWithSource;

        if (hasPartialPositional)
            return CliPathResolution.Fail(Usage.ErrorIncompletePositional);

        if (hasPartialNamed)
            return CliPathResolution.Fail(Usage.ErrorIncompleteNamed);

        if (hasPositional && hasNamed)
            return CliPathResolution.Fail(Usage.ErrorPositionalAndNamed);

        if (hasPositional)
            return CliPathResolution.Ok(exportPos!, solutionPos, gacPatterns);

        if (hasNamed)
            return CliPathResolution.Ok(exportOpt!, inputOpt, gacPatterns);

        return CliPathResolution.Fail(null);
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
