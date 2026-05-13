# Step 05 — CLI: DLL/EXE als Input dokumentieren und validieren

## Ziel

Nutzer und Agenten sollen an der CLI erkennen: **Eingaben** sind nicht nur Verzeichnisse, sondern auch **kompilierte .NET-Assemblies** (`.dll`/`.exe`). Ungültige Pfade sollen **früh** mit klaren Fehlermeldungen scheitern.

## Kontext

- Datei: `SourceToAI.CLI/App/Cli/SourceToAiCli.cs` (Usage-Konstanten, `SolutionRootDescription`, ggf. `RootDescription`).
- Parsing: `ResolveInvocation` / `NormalizePathList` — optional **zusätzliche** Validierung erst in `Program.RunExportPipelineAsync` **vor** DI, wenn ihr dort Zugriff auf die Pfadliste habt; alternativ nur in `ConsoleOrchestrator` (bereits Step 03). **Mindestens eine** Stelle muss sicherstellen, dass weder existierende **falsche** Dateitypen (z. B. `.txt`) noch nicht existierende Pfade still durchrutschen.

## Aufgaben

1. **Texte anpassen** (`Usage`, Beschreibungen):
   - Statt nur „Solution/Repository-Stamm“ sinngemäß: Verzeichnis **oder** `.dll`/`.exe` (Pfad zur Assembly).
   - `UsageLine` / Beispiele um **ein Assembly-Beispiel** ergänzen (Windows-Pfad-Stil ok, generisch halten).
2. **Validierung (empfohlen in `RunExportPipelineAsync` nach `parseResult`):**
   - Für jeden Eintrag in `solutionPaths`:  
     - Wenn `File.Exists` und Extension `.dll`/`.exe` → ok.  
     - Wenn `Directory.Exists` → ok.  
     - Sonst: Fehlermeldung auf **stderr**, Exitcode `1`, keine DI-Auflösung nötig.
3. **`SourceToAI.Tests/App/SourceToAiCliTests.cs`** (falls vorhanden) oder neue Testdatei: Parser akzeptiert weiter Positional/Named; **kein** Bruch der bestehenden Szenarien.

## Nicht-Ziele

- Keine neue CLI-Flag-Struktur, solange Positional + `--input` ausreichen.

## Abhaken (Pflicht am Step-Ende)

- [ ] **Step 05 abgehackt** → `- [X] **Step 05 abgehackt**`
