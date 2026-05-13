# Step 05 — `MultiViewReadmeMarkdownGenerator`: Struktur `Isolated` / `Merged` und Datei-Suffix

## Ziel

Die generierte `readme.md` (wird nach Step 03 unter `{Export-Pfad}/readme.md` abgelegt) erklärt:

- **`Merged/`** — alle Projekte aller Solutions, nach View sortiert; Nutzen für KI-Prompts über den ganzen Workspace.
- **`Isolated/`** — klassische Gruppierung pro Solution; darunter u. a. `dependency-graph.md` pro Solution.
- **Dateinamen** — Stamm enthält Suffix `-<view>` (z. B. `-complete`, `-dto-only`), passend zu Step 01/02.

## Voraussetzungen

- [Step 03](03-console-orchestrator-global-export-root.md) legt die Datei ins globale Export-Root (Pfad-Seite).
- Text in diesem Step soll mit der echten Ordner-/Dateistruktur übereinstimmen.

## Betroffene Dateien

- `SourceToAI.CLI/Services/Export/MultiViewReadmeMarkdownGenerator.cs`
- Folgetests: `SourceToAI.Tests/Export/MultiViewReadmeMarkdownGeneratorTests.cs` (Step 07)

## Ausgangscode

Die Tabelle und Sätze beziehen sich noch auf flache View-Ordner und `Solution.Projekt.md`:

```21:32:SourceToAI.CLI/Services/Export/MultiViewReadmeMarkdownGenerator.cs
        sb.AppendLine("Jede `.md` direkt unter einem **View-Ordner** (`complete/`, `signatures-only/`, `public-only/`, `dto-only/`) beschreibt **genau ein** exportiertes Projekt — inklusive YAML-Frontmatter, **MANIFEST**-Tabelle ...
        ...
        sb.AppendLine("| `dependency-graph.md` | **Solution-Ebene** (gleiche Verzeichnisebene wie diese `readme.md`): Architektur-Überblick ...
        sb.AppendLine("| `complete/<Solution>.<Projekt>.md` | ...
```

## Aufgaben

1. **Einleitung / Meta:** Titel kann weiter `repositoryRootFolderName` nutzen (pro Lauf vom Orchestrator gesetzt) — oder um einen Hinweis auf „globale Readme“ ergänzen, ohne die Signatur `Generate(string, DateTimeOffset)` zwingend zu ändern.
2. **Markdown-Tabelle** umbauen:
   - Zeile für `readme.md` im Export-Root.
   - Zeile für `Isolated/<Solution>/dependency-graph.md` (nicht mehr „gleiche Ebene wie readme“).
   - Zeilen für View-Pfade unter **`Merged/<view>/`** und **`Isolated/<Solution>/<view>/`** mit Platzhalter-Dateinamen `Solution.Projekt-<view>.md` (view = `complete` etc. im Suffix).
3. **Hinweise-Abschnitt:** erwähnen, dass dieselbe Projekt-View-Datei in `Merged` und `Isolated` identisch ist (Duplikat zwecks Prompt-Workflow).
4. Keine falschen Verweise auf entfernte Pfade (`full-source.md` weiter nicht erwähnen — Tests prüfen `DoesNotContain`).

## Abnahme

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --filter "FullyQualifiedName~MultiViewReadmeMarkdownGeneratorTests"
```

(Der Lauf kann erst nach Anpassung der Assertions in Step 07 komplett grün sein — Reihenfolge: zuerst Generator-Text, dann Tests.)

## Referenzen

- [`konzept.md`](konzept.md) — Abschnitt 4 und Textbausteine.
- [`MultiViewReadmeMarkdownGenerator.cs`](../../SourceToAI.CLI/Services/Export/MultiViewReadmeMarkdownGenerator.cs)
