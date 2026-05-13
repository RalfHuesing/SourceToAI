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
            "Exportiert eine oder mehrere .NET-Quellen (Solution-/Projektverzeichnis oder kompilierte Assembly .dll/.exe) als Multi-View-KI-Feed (Markdown) nacheinander in dasselbe Export-Verzeichnis.";

        internal const string UsageLine =
            "Verwendung: SourceToAI <Export-Pfad> <Verzeichnis|.dll|.exe>… | SourceToAI --export <Export-Pfad> --input <Pfad>…";

        internal const string UsageExamplePositional =
            "Beispiel (Positionsargumente): SourceToAI ./exports C:\\Daten\\RepoA\\ C:\\Daten\\RepoB\\";

        internal const string UsageExampleAssembly =
            "Beispiel (Assembly): SourceToAI ./exports C:\\Apps\\MyLib\\bin\\Debug\\net10.0\\MyLib.dll";

        internal const string UsageExampleOptions =
            "Beispiel (Optionen): SourceToAI --export ./exports --input C:\\Daten\\RepoA\\ --input C:\\Daten\\RepoB\\";

        internal const string ExportPathDescription =
            "Zielverzeichnis für den Export (wird bei Bedarf angelegt bzw. geleert).";

        internal const string SolutionRootDescription =
            "Quellverzeichnis (Solution/Repository mit .sln oder darüber) oder Pfad zu einer .NET-Assembly (.dll/.exe). Mindestens ein Pfad; weitere Pfade als weitere Positionsargumente.";

        internal const string ExportOptionDescription =
            "Export-Verzeichnis (Alternative zu Positionsargument <Export-Pfad>).";

        internal const string InputOptionDescription =
            "Quellverzeichnis oder Assembly (.dll/.exe); Alternative zu den weiteren Positionsargumenten. Mehrfach angebbare Option.";

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
        Func<string, IReadOnlyList<string>, CancellationToken, Task<int>> runAsync)
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

        var root = new RootCommand(Usage.RootDescription)
        {
            TreatUnmatchedTokensAsErrors = true,
        };
        root.Add(exportPositional);
        root.Add(solutionPositional);
        root.Add(exportOption);
        root.Add(inputOption);

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
                await Console.Error.WriteLineAsync(Usage.UsageExampleOptions);
                return 1;
            }

            return await runAsync(resolution.ExportPath!, resolution.SolutionPaths, cancellationToken);
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
