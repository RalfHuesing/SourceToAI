namespace SourceToAI.CLI.Infrastructure;

/// <summary>Stabile View-Schlüssel für Markdown-Export und Keyed-DI (müssen zu den <c>ViewKey</c>-Werten der View-Generatoren passen).</summary>
public static class MarkdownViewKeys
{
    public const string Complete = "complete";
    public const string SignaturesOnly = "signatures-only";
    public const string PublicOnly = "public-only";
    public const string DtoOnly = "dto-only";
}
