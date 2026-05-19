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
            "Exportiert eine oder mehrere .NET-Quellen (Solution-/Projektverzeichnis oder kompilierte Assembly .dll/.exe) als Multi-View-KI-Feed (Markdown) nacheinander in dasselbe Export-Verzeichnis. Optional: wiederholbare Option --exclude mit Glob-Mustern relativ zum jeweiligen Projektstamm.";

        internal const string UsageLine =
            "Verwendung: SourceToAI <Export-Pfad> <Verzeichnis|.dll|.exe>… | SourceToAI --export <Export-Pfad> --input <Pfad>… [--exclude <Glob>…]";

        internal const string UsageExamplePositional =
            "Beispiel (Positionsargumente): SourceToAI ./exports C:\\Daten\\RepoA\\ C:\\Daten\\RepoB\\";

        internal const string UsageExampleAssembly =
            "Beispiel (Assembly): SourceToAI ./exports C:\\Apps\\MyLib\\bin\\Debug\\net10.0\\MyLib.dll";

        internal const string UsageExampleAssemblyWildcard =
            "Beispiel (Assemblies mit Platzhalter * / ? im Dateinamen): SourceToAI ./exports C:\\Apps\\MyLib\\bin\\Debug\\net10.0\\*.dll";

        internal const string UsageExampleOptions =
            "Beispiel (Optionen): SourceToAI --export ./exports --input C:\\Daten\\RepoA\\ --input C:\\Daten\\RepoB\\";

        internal const string UsageExampleExclude =
            "Beispiel (Ausschluss per Glob, relativ zum jeweiligen Projektordner): SourceToAI ./exports C:\\Repo\\ --exclude \"wwwroot/lib/**\" --exclude \"**/vis-timeline-graph2d.min.js\"";

        internal const string ExportPathDescription =
            "Zielverzeichnis für den gesamten Export-Baum (wird bei Bedarf angelegt; bestehender Inhalt wird sicherheitshalber geleert, sofern .sta-marker existiert).";

        internal const string SolutionRootDescription =
            "Quellverzeichnis (Solution/Repository mit .sln oder darüber) oder Pfad zu einer .NET-Assembly (.dll/.exe). Mindestens ein Pfad; weitere Pfade als weitere Positionsargumente.";

        internal const string ExportOptionDescription =
            "Export-Verzeichnis (Alternative zu Positionsargument <Export-Pfad>).";

        internal const string InputOptionDescription =
            "Quellverzeichnis oder Assembly (.dll/.exe); Alternative zu den weiteren Positionsargumenten. Mehrfach angebbare Option.";

        internal const string ExcludeOptionDescription =
            "Glob-Muster für auszuschließende Dateien/Unterbäume (Microsoft.Extensions.FileSystemGlobbing). Relativ zum jeweiligen Projektstamm (Ordner der .csproj) und zusätzlich zur Solution-/Eingabe-Wurzel (z. B. Leitstand.VBA.Addin für Ordner direkt unter der Wurzel). Mehrfach angebbbar. Unterbaum: wwwroot/lib/** oder Ordnername ohne Wildcards.";

        internal const string ErrorIncompletePositional =
            "Positionsargument <Export-Pfad> und mindestens ein Quellpfad (Verzeichnis oder .dll/.exe) sind erforderlich, wenn ohne --export/--input gearbeitet wird.";

        internal const string ErrorIncompleteNamed =
            "Option --export und mindestens ein --input sind erforderlich, wenn ohne Positionsargumente gearbeitet wird.";

        internal const string ErrorPositionalAndNamed =
            "Positionsargumente und --export/--input dürfen nicht kombiniert werden.";
    }

    /// <summary>
    /// Erzeugt den Root-Command; <paramref name="runAsync"/> wird bei gültiger CLI aufgerufen.
    /// </summary>
    internal static RootCommand CreateRootCommand(
        Func<string, IReadOnlyList<string>, IReadOnlyList<string>, CancellationToken, Task<int>> runAsync)
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
        var excludeOption = new Option<string[]>("--exclude")
        {
            Description = Usage.ExcludeOptionDescription,
            Arity = ArgumentArity.OneOrMore,
        };

        var root = new RootCommand(Usage.RootDescription)
        {
            TreatUnmatchedTokensAsErrors = true,
        };
        root.Add(exportPositional);
        root.Add(solutionPositional);
        root.Add(exportOption);
        root.Add(inputOption);
        root.Add(excludeOption);

        root.SetAction(async (ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            var resolution = ResolveInvocation(
                parseResult,
                exportPositional,
                solutionPositional,
                inputOption,
                exportOption);
            if (resolution.ExportPath is null || resolution.SolutionPaths.Count == 0)
            {
                if (resolution.ErrorMessage is not null)
                    await Console.Error.WriteLineAsync(resolution.ErrorMessage);
                await Console.Error.WriteLineAsync(Usage.UsageLine);
                await Console.Error.WriteLineAsync(Usage.UsageExamplePositional);
                await Console.Error.WriteLineAsync(Usage.UsageExampleAssembly);
                await Console.Error.WriteLineAsync(Usage.UsageExampleAssemblyWildcard);
                await Console.Error.WriteLineAsync(Usage.UsageExampleOptions);
                await Console.Error.WriteLineAsync(Usage.UsageExampleExclude);
                return 1;
            }

            string? TrimTok(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            var excludeRaw = parseResult.GetValue(excludeOption);
            var excludePatterns = NormalizePathList(excludeRaw ?? Array.Empty<string>(), TrimTok);

            return await runAsync(resolution.ExportPath!, resolution.SolutionPaths, excludePatterns, cancellationToken);
        });

        return root;
    }

    private static CliPathResolution ResolveInvocation(
        ParseResult parseResult,
        Argument<string?> exportPositional,
        Argument<string[]> solutionPositional,
        Option<string[]> inputOption,
        Option<string?> exportOption)
    {
        static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var exportPos = N(parseResult.GetValue(exportPositional));
        var solutionPosRaw = parseResult.GetValue(solutionPositional);
        var solutionPos = NormalizePathList(solutionPosRaw, N);
        var exportOpt = N(parseResult.GetValue(exportOption));
        var inputOptRaw = parseResult.GetValue(inputOption);
        var inputOpt = NormalizePathList(inputOptRaw, N);

        var hasExportPos = exportPos is not null;
        var hasSolutionPos = solutionPos.Count > 0;
        var hasPositional = hasExportPos && hasSolutionPos;
        var hasPartialPositional = hasExportPos ^ hasSolutionPos;

        var hasExportOpt = exportOpt is not null;
        var hasInputOpt = inputOpt.Count > 0;
        var hasNamed = hasExportOpt && hasInputOpt;
        var hasPartialNamed = hasExportOpt ^ hasInputOpt;

        if (hasPartialPositional)
            return CliPathResolution.Fail(Usage.ErrorIncompletePositional);

        if (hasPartialNamed)
            return CliPathResolution.Fail(Usage.ErrorIncompleteNamed);

        if (hasPositional && hasNamed)
            return CliPathResolution.Fail(Usage.ErrorPositionalAndNamed);

        if (hasPositional)
            return CliPathResolution.Ok(exportPos!, solutionPos);

        if (hasNamed)
            return CliPathResolution.Ok(exportOpt!, inputOpt);

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
        private CliPathResolution(string? exportPath, IReadOnlyList<string> solutionPaths, string? errorMessage)
        {
            ExportPath = exportPath;
            SolutionPaths = solutionPaths;
            ErrorMessage = errorMessage;
        }

        internal string? ExportPath { get; }
        internal IReadOnlyList<string> SolutionPaths { get; }
        internal string? ErrorMessage { get; }

        internal static CliPathResolution Ok(string exportPath, IReadOnlyList<string> solutionPaths) =>
            new(exportPath, solutionPaths, null);

        internal static CliPathResolution Fail(string? errorMessage) =>
            new(null, Array.Empty<string>(), errorMessage);
    }
}
