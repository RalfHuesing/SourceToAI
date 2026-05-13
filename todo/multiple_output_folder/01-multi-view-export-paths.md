# Step 01 — `MultiViewExportPaths`: Konstanten, Stamm mit View-Suffix, isolierte Wurzel

## Ziel

Zentrale Pfad- und Dateinamenlogik für die neue Struktur (`Isolated/`, `Merged/`, Dateiname `Solution.Projekt-<viewKey>.md`) in einer Datei bündeln, damit Orchestrator und Export-Service dieselben Konstanten nutzen.

## Voraussetzungen

- Keine (erster Implementierungsschritt).
- Kontext: [`konzept.md`](konzept.md) Abschnitt „1. MultiViewExportPaths.cs“.

## Betroffene Datei

- `SourceToAI.CLI/Services/Export/MultiViewExportPaths.cs`

## Aufgaben

### 1. Neue öffentliche Konstanten

- `IsolatedFolderName = "Isolated"`
- `MergedFolderName = "Merged"`

(Schreibweise exakt wie in `konzept.md`, damit Ordner auf Platte konsistent sind.)

### 2. `GetSolutionExportRoot` semantisch anpassen

Aktuell (vereinfacht):

```56:57:SourceToAI.CLI/Services/Export/MultiViewExportPaths.cs
    public static string GetSolutionExportRoot(string exportPath, string solutionName) =>
        Path.Combine(exportPath, solutionName);
```

**Neu:** Rückgabe = Basis für alles, was *pro Solution* unter `Isolated/` liegt (readme/dependency-graph waren bisher *direkt* unter `{export}/{solution}` — der Orchestrator wechselt die Aufrufer später auf dieses neue Segment).

Vorschlag für die Kombination:

- `Path.Combine(exportPath, IsolatedFolderName, solutionName)`

**Hinweis für Folgeschritte:** Assembly-Pfad im `ConsoleOrchestrator` vergleicht heute `GetSolutionExportRoot(exportPath, solutionName)` mit `plannedSolutionExportRoot` — nach dieser Änderung muss `plannedSolutionExportRoot` dieselbe Konvention nutzen (siehe Step 03).

### 3. `BuildSanitizedExportFileStem` um `viewKey` erweitern

Aktuell nur Solution + Projekt:

```108:115:SourceToAI.CLI/Services/Export/MultiViewExportPaths.cs
    public static string BuildSanitizedExportFileStem(string solutionDisplayName, string projectDisplayName)
    {
        var sol = SanitizeFileNameSegment(solutionDisplayName);
        var proj = SanitizeFileNameSegment(projectDisplayName);
        var stem = $"{sol}.{proj}";
        stem = EnsureNotReservedWindowsStem(stem);
        return stem;
    }
```

**Neu:** dritter Parameter `string viewKey` (oder explizit `viewKey` nach Sanitization), Ergebnis z. B.:

- `EnsureNotReservedWindowsStem($"{sol}.{proj}-{SanitizeFileNameSegment(viewKey)}")`

Damit entstehen Stämme wie `MySol.MyProj-complete` (Ordner `complete/` bleibt; der **Dateiname** trägt das View-Suffix, wie im Konzept).

**Breaking change:** Alle Aufrufer der zweiparametrigen Variante anpassen (Step 02 + ggf. Tests in Step 06+).

### 4. Überladung `GetViewOutputPath(..., solution, project)` anpassen

Die vierparametrige Überladung ruft intern `BuildSanitizedExportFileStem` auf — sie braucht dann ebenfalls einen `viewKey` **oder** wird auf die dreiparametrige Form reduziert, sobald der Stamm nur noch mit View gebaut wird.

Empfehlung: Signatur um `viewKey` erweitern **oder** die vierparametrige Überladung entfernen, falls sie nirgends mehr passt (Repo per `rg BuildSanitizedExportFileStem` / `GetViewOutputPath` prüfen).

### 5. XML-Dokumentation in derselben Datei aktualisieren

Der große `<remarks>`-Block beschreibt noch `{exportPath}/{solutionName}` und `Solution.Project.md` — auf `Isolated/`, `Merged/` und Suffix `-<view>` abstimmen, damit IDE und spätere Leser nicht irregeführt werden.

## Abnahme (lokal)

```bash
dotnet build SourceToAI.CLI/SourceToAI.CLI.csproj
```

Erwartung: Build grün; **Tests können noch rot sein**, bis Step 02/06.

## Referenzen

- [`konzept.md`](konzept.md) — Zielstruktur und Punkt 1.
- [`MultiViewExportPaths.cs`](../../SourceToAI.CLI/Services/Export/MultiViewExportPaths.cs)
- Aufrufer Stamm: [`MultiViewExportService.cs`](../../SourceToAI.CLI/Services/Export/MultiViewExportService.cs) Zeilen 132–135 (wird in Step 02 geändert).
