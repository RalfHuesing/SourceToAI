#nullable enable
using System.Collections.Generic;

namespace SourceToAI.CLI.Models;

/// <summary>
/// Repräsentiert eine einzelne Export-Einheit (Projekt oder Partition) für das Schreiben von Markdown-Feeds.
/// </summary>
public record ExportUnit(ProjectDefinition Project, IReadOnlyList<string> Paths, bool DocsOnlyInCompleteView);
