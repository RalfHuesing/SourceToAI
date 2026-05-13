# Step 12 (optional) — Projektdokumentation & CLI-Texte (`README.md`, `SourceToAiCli`)

## Ziel

Öffentliche Doku und Usage-Beschreibungen an die neue Ordnerstruktur anpassen, damit Nutzer nicht zwischen README und generierter `readme.md` im alten Format verwirrt werden.

## Voraussetzungen

- Kernfunktion und Tests sind grün ([Step 11](11-abschluss-tests-und-suche.md)).

## Betroffene Dateien (Vorschlag)

- `README.md` — aktuell u. a. Zeilen zu Multi-View-Pfaden und `{Export}/{AssemblyName}/`:

```13:15:README.md
- **Multi-View-Export:** Unter `complete/`, ...
- **Mehrere Quellen in einem Lauf:** ... pro erkanntem Solution-Namen entsteht ein **eigener Unterordner** unter dem Export-Pfad.
- **Quellen:** ... `decompile/`-Projekt unter `{Export}/{AssemblyName}/` ...
```

- `SourceToAI.CLI/App/Cli/SourceToAiCli.cs` — `Usage.ExportPathDescription` („wird bei Bedarf angelegt bzw. **geleert**“) ist weiterhin richtig, aber ggf. präzisieren, dass das **gesamte** Export-Ziel betroffen ist.

## Aufgaben

1. README: neue Baumstruktur in Kurzform (`Isolated/`, `Merged/`, globales `readme.md`, Suffixe).
2. README: Abschnitt „Nutzung mit Web-KIs“ — Pfade zu `Merged/complete/...` oder `Isolated/...` anpassen.
3. `SourceToAiCli`: nur wenn sich das Nutzerversprechen ändert (z. B. mehrere Solutions schreiben in **denselben** vorbereiteten Baum ohne pro-Solution-Löschen).

## Abnahme

- Kein zwingender Testlauf; bei Änderung an `SourceToAiCli` kurz `dotnet build`.

## Referenzen

- [`README.md`](../../README.md)
- [`SourceToAiCli.cs`](../../SourceToAI.CLI/App/Cli/SourceToAiCli.cs)
