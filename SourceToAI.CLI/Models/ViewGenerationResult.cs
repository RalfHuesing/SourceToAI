namespace SourceToAI.CLI.Models;

/// <summary>Ergebnis eines View-Generators: Ausgabetext plus AST-basierte Exportierbarkeit für den AI-Feed.</summary>
public sealed record ViewGenerationResult(string OutputText, bool HasExportableSurface);
