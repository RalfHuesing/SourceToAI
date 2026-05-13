# Step 04 — `IMultiViewExportService`: XML-Dokumentation an neue Semantik anpassen

## Ziel

Die öffentliche Schnittstelle beschreibt korrekt:

- wo die Dateien landen (`Isolated/…` und `Merged/…`);
- dass der Dateiname das View-Suffix enthält (`-<viewKey>` im Stamm vor `.md`);
- dass `outputRoot` der **globale** Export-Pfad ist.

## Voraussetzungen

- [Step 02](02-multi-view-export-service-dual-write.md) erledigt (Implementierung und reale Pfade stimmen mit Doku überein).

## Betroffene Datei

- `SourceToAI.CLI/Services/Export/IMultiViewExportService.cs`

## Ausgangstext (korrigieren)

Aktuell u. a.:

```8:25:SourceToAI.CLI/Services/Export/IMultiViewExportService.cs
public interface IMultiViewExportService
{
    /// <summary>
    /// Schreibt die zusammengeführten Multi-View-Markdown-Dateien (ein Baum pro Solution-Lauf).
    /// </summary>
    ...
    /// eine Markdown-Datei unter <see cref="MultiViewExportPaths.GetViewFolderNameForViewKey"/> geschrieben
    /// (<c>SolutionName.ProjektName.md</c>, siehe <see cref="MultiViewExportPaths"/>).
```

## Aufgaben

- `<summary>`: z. B. erwähnen, dass pro View **zwei** gleichnamige Dateien (gleicher Inhalt) unter `Isolated/{Solution}/…` und `Merged/…` entstehen können.
- `<param name="outputRoot">`: explizit „globaler Export-Pfad (Argument `<Export-Pfad>` der CLI), nicht die isolierte Solution-Wurzel“.
- Verweis auf `MultiViewExportPaths.IsolatedFolderName` / `MergedFolderName` optional per `<see cref="…"/>`.
- `<c>SolutionName.ProjektName.md</c>` → Muster mit Suffix, konsistent zu Step 01.

## Abnahme

```bash
dotnet build SourceToAI.CLI/SourceToAI.CLI.csproj
```

## Referenzen

- [`IMultiViewExportService.cs`](../../SourceToAI.CLI/Services/Export/IMultiViewExportService.cs)
- [`konzept.md`](konzept.md) Abschnitt 3.
