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
            "Exportiert eine .NET-Solution als Multi-View-KI-Feed (Markdown).";

        internal const string UsageLine =
            "Verwendung: SourceToAI <Export-Pfad> <Pfad-zur-Solution> | SourceToAI --export <Export-Pfad> --input <Pfad-zur-Solution>";

        internal const string UsageExamplePositional =
            "Beispiel (Positionsargumente): SourceToAI ./exports C:\\Daten\\MeineSolution\\";

        internal const string UsageExampleOptions =
            "Beispiel (Optionen): SourceToAI --export ./exports --input C:\\Daten\\MeineSolution\\";

        internal const string ExportPathDescription =
            "Zielverzeichnis für den Export (wird bei Bedarf angelegt bzw. geleert).";

        internal const string SolutionRootDescription =
            "Stammverzeichnis der Solution (Ordner mit .sln oder darüber).";

        internal const string ExportOptionDescription =
            "Export-Verzeichnis (Alternative zu Positionsargument <Export-Pfad>).";

        internal const string InputOptionDescription =
            "Pfad zur Solution bzw. zum Repository-Stamm (Alternative zum zweiten Positionsargument). Wiederholbare Angabe mehrerer Pfade ist für eine spätere Version vorgesehen.";

        internal const string ErrorIncompletePositional =
            "Beide Positionsargumente <Export-Pfad> und <Pfad-zur-Solution> sind erforderlich, wenn ohne --export/--input gearbeitet wird.";

        internal const string ErrorIncompleteNamed =
            "Beide Optionen --export und --input sind erforderlich, wenn ohne Positionsargumente gearbeitet wird.";

        internal const string ErrorPositionalAndNamed =
            "Positionsargumente und --export/--input dürfen nicht kombiniert werden.";
    }

    /// <summary>
    /// Erzeugt den Root-Command; <paramref name="runAsync"/> wird bei gültiger CLI aufgerufen.
    /// </summary>
    internal static RootCommand CreateRootCommand(
        Func<string, string, CancellationToken, Task<int>> runAsync)
    {
        var exportPositional = new Argument<string?>("export-path")
        {
            Description = Usage.ExportPathDescription,
            Arity = ArgumentArity.ZeroOrOne,
        };
        var solutionPositional = new Argument<string?>("solution-root")
        {
            Description = Usage.SolutionRootDescription,
            Arity = ArgumentArity.ZeroOrOne,
        };

        var exportOption = new Option<string?>("--export", [])
        {
            Description = Usage.ExportOptionDescription,
        };
        var inputOption = new Option<string?>("--input", ["-i"])
        {
            Description = Usage.InputOptionDescription,
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
            if (resolution.ExportPath is null || resolution.SolutionPath is null)
            {
                if (resolution.ErrorMessage is not null)
                    await Console.Error.WriteLineAsync(resolution.ErrorMessage);
                await Console.Error.WriteLineAsync(Usage.UsageLine);
                await Console.Error.WriteLineAsync(Usage.UsageExamplePositional);
                await Console.Error.WriteLineAsync(Usage.UsageExampleOptions);
                return 1;
            }

            return await runAsync(resolution.ExportPath!, resolution.SolutionPath!, cancellationToken);
        });

        return root;
    }

    private static CliPathResolution ResolveInvocation(
        ParseResult parseResult,
        Argument<string?> exportPositional,
        Argument<string?> solutionPositional,
        Option<string?> inputOption,
        Option<string?> exportOption)
    {
        static string? N(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var exportPos = N(parseResult.GetValue(exportPositional));
        var solutionPos = N(parseResult.GetValue(solutionPositional));
        var exportOpt = N(parseResult.GetValue(exportOption));
        var inputOpt = N(parseResult.GetValue(inputOption));

        var hasPositional = exportPos is not null && solutionPos is not null;
        var hasPartialPositional = exportPos is not null ^ solutionPos is not null;
        var hasNamed = exportOpt is not null && inputOpt is not null;
        var hasPartialNamed = exportOpt is not null ^ inputOpt is not null;

        if (hasPartialPositional)
            return CliPathResolution.Fail(Usage.ErrorIncompletePositional);

        if (hasPartialNamed)
            return CliPathResolution.Fail(Usage.ErrorIncompleteNamed);

        if (hasPositional && hasNamed)
            return CliPathResolution.Fail(Usage.ErrorPositionalAndNamed);

        if (hasPositional)
            return CliPathResolution.Ok(exportPos!, solutionPos!);

        if (hasNamed)
            return CliPathResolution.Ok(exportOpt!, inputOpt!);

        return CliPathResolution.Fail(null);
    }

    private readonly struct CliPathResolution
    {
        private CliPathResolution(string? exportPath, string? solutionPath, string? errorMessage)
        {
            ExportPath = exportPath;
            SolutionPath = solutionPath;
            ErrorMessage = errorMessage;
        }

        internal string? ExportPath { get; }
        internal string? SolutionPath { get; }
        internal string? ErrorMessage { get; }

        internal static CliPathResolution Ok(string exportPath, string solutionPath) =>
            new(exportPath, solutionPath, null);

        internal static CliPathResolution Fail(string? errorMessage) =>
            new(null, null, errorMessage);
    }
}
