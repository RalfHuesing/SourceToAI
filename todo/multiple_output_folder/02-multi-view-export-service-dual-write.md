# Step 02 — `MultiViewExportService`: Stamm mit View, doppeltes Schreiben (Isolated + Merged)

## Ziel

Pro fertig komponiertem View-Body die Markdown-Datei **zweimal** schreiben:

1. **Isolated:** `{outputRoot}/Isolated/{solutionDisplayName}/{viewFolder}/{stem}.md`
2. **Merged:** `{outputRoot}/Merged/{viewFolder}/{stem}.md`

Dabei ist `outputRoot` der **globale** Export-Pfad (siehe Step 03); `stem` enthält das View-Suffix (Step 01).

## Voraussetzungen

- [Step 01 — MultiViewExportPaths](01-multi-view-export-paths.md) ist umgesetzt (Konstanten + `BuildSanitizedExportFileStem(..., viewKey)` + angepasstes `GetSolutionExportRoot` falls vom Service indirekt genutzt).

## Betroffene Dateien

- `SourceToAI.CLI/Services/Export/MultiViewExportService.cs`
- optional: `SourceToAI.CLI/Services/Export/IMultiViewExportService.cs` nur **XML-Kommentare** (ausführlicher Step 04 möglich — hier schon prüfen, ob Kommentar noch `SolutionName.ProjektName.md` behauptet).

## Ausgangscode (relevant)

Schleife und einfacher Schreibpfad:

```122:176:SourceToAI.CLI/Services/Export/MultiViewExportService.cs
        for (var i = 0; i < workSlots.Count; i++)
        {
            var body = composedBodies[i];
            if (body is null)
                continue;

            var slot = workSlots[i];
            var viewFolder = MultiViewExportPaths.GetViewFolderNameForViewKey(slot.ViewKey);
            var usedStems = usedStemsPerView[slot.ViewKey];

            var stem = MultiViewExportPaths.AllocateUniqueFileStem(
                MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, slot.Project.ProjectName),
                usedStems);
            WriteProjectViewFile(outputRoot, viewFolder, stem, body);
        }
```

```168:176:SourceToAI.CLI/Services/Export/MultiViewExportService.cs
    private static void WriteProjectViewFile(string outputRoot, string viewFolder, string uniqueStem, string body)
    {
        var outPath = MultiViewExportPaths.GetViewOutputPath(outputRoot, viewFolder, uniqueStem);
        ...
        File.WriteAllText(outPath, body);
    }
```

## Aufgaben

### 1. Stamm mit View-Key

- `BuildSanitizedExportFileStem(solutionDisplayName, slot.Project.ProjectName, slot.ViewKey)` verwenden.
- `usedStemsPerView` bleibt **pro View-Key** sinnvoll — Kollisionen werden weiterhin pro View-Ordner aufgelöst; durch das Suffix sind Stämme ohnehin stabiler.

### 2. Zwei Zielpfade pro Schreibvorgang

Entweder:

- `WriteProjectViewFile` zu einer Methode erweitern, die zwei absolute Pfade schreibt (DRY: `Directory.CreateDirectory` + `File.WriteAllText` in einer Hilfsmethode `WriteTextEnsuringDirectory(string path, string content)`), **oder**
- zweimal `GetViewOutputPath` mit unterschiedlichem „Root“-Segment aufrufen:
  - Isolated-Root: `Path.Combine(outputRoot, MultiViewExportPaths.IsolatedFolderName, solutionDisplayName)`
  - Merged-Root: `Path.Combine(outputRoot, MultiViewExportPaths.MergedFolderName)`

**Wichtig:** `solutionDisplayName` für den Ordnernamen unter `Isolated/` sollte konsistent mit dem sein, was der Orchestrator für Pfade verwendet (gleiche Zeichen/Sanitization-Frage: aktuell unge-sanitized Solution-Name — wenn der Dateisystem gefährdet, in einem späteren Mini-Step `SanitizeFileNameSegment` nur für den **Ordner** prüfen; das Konzept nennt `<SolutionA>` als Klartext).

### 3. Kein blockierendes I/O in Locks

Projektrichtlinie: kein `ReadAllText`/`ParseText` in `lock` — hier nur `WriteAllText`; falls parallelisiert wird, aktuell schreibt die Schleife sequentiell (unverändert lassen, es sei denn, ihr refactort bewusst — dann Parallelität + Reihenfolge dokumentieren).

### 4. `WriteMergedSolutionViews`-Parameter `outputRoot`

Die Methode erhält künftig den **globalen** Export-Pfad (nicht mehr `{export}/{solution}`). Die Signatur kann gleich bleiben; Semantik in XML-Doku anpassen (Step 04).

## Abnahme

```bash
dotnet build SourceToAI.CLI/SourceToAI.CLI.csproj
```

Unit-Tests folgen in den Test-Steps; wer hier manuell prüfen will: einmal CLI mit Mini-Solution ausführen **nach** Step 03, sonst fehlen Ordner/Marker-Logik.

## Referenzen

- [`konzept.md`](konzept.md) — Abschnitt 3 (zweifaches Schreiben).
- [`MultiViewExportService.cs`](../../SourceToAI.CLI/Services/Export/MultiViewExportService.cs)
- [`MultiViewExportPaths.cs`](../../SourceToAI.CLI/Services/Export/MultiViewExportPaths.cs) nach Step 01.
