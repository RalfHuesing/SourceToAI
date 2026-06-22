using System;

namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Bündelt Session-Metadaten für die Generierung von AI Feeds.
/// </summary>
public sealed record AiFeedSessionInfo(
    string SolutionDisplayName,
    Guid SessionId,
    DateTimeOffset Generated);
