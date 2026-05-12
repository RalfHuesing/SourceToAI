namespace SourceToAI.CLI.Services.Export.AiFeed;

/// <summary>
/// Ein gefiltertes Inhaltssegment für den AI-Feed (nach View-Transformation; Manifest-Spalten werden zentral im Composer abgeleitet).
/// </summary>
/// <param name="RelativePathFromProjectRoot">Relativer Pfad; Darstellung in Manifest/Überschrift über <see cref="AiFeedManifestPath"/>.</param>
/// <param name="FileTypeCategory">Kategorie z. B. aus dem File-Type-Service (<c>Doc</c> → Manifest-Typ Doc, sonst Code).</param>
/// <param name="FenceLanguage">Info-String direkt nach der öffnenden Fence (z. B. <c>csharp</c>, <c>markdown</c>); leer erlaubt.</param>
/// <param name="TransformedText">Text im CONTENT (Hash/Size beziehen sich darauf).</param>
/// <param name="CSharpRewrittenHasExportableSurface">
/// Bei <see cref="AiFeedTransformedContentKind.RewrittenViewOutput"/> und <c>csharp</c>: vom View-Generator gesetzt (AST-basiert, ohne erneutes Parsen).
/// Sonst <c>null</c>.
/// </param>
public sealed record AiFeedContentSegment(
    string RelativePathFromProjectRoot,
    string FileTypeCategory,
    string FenceLanguage,
    string TransformedText,
    bool? CSharpRewrittenHasExportableSurface = null);
