# Step 06 — Tests: Decompiler, Discovery, Orchestrator, CLI

## Ziel

Automatisierte Tests absichern, sodass Regressionen bei Assembly-Eingabe, Decompile-Pfad und Solution-Namen ausgeschlossen sind. **`dotnet test`** auf der Solution muss **grün** sein, **0** relevante Compiler-Warnungen.

## Kontext

- Testprojekt: `SourceToAI.Tests` (`net10.0`, xUnit v3, Moq).
- CLI ist referenziert; `InternalsVisibleTo` erlaubt ggf. Internals — nur nutzen, wenn schon üblich im Projekt.

## Aufgaben (priorisiert)

### 1. `AssemblyDecompilerService` / Interface

- **Integrationstest** (empfohlen): Eine **minimale echte** Assembly erzeugen **ohne** ILSpy-Mock:
  - Im Test `Path.GetTempPath()` + GUID-Unterordner.
  - Optional: `Roslyn`/`Microsoft.CodeAnalysis` ist bereits im Testprojekt — kleines Snippet kompilieren zu **einer** `.dll` im Temp (oder vorgefertigte Test-Ressource einchecken, wenn zu aufwendig — bevorzugt: dynamisch generieren, keine großen Binaries im Repo).
  - `AssemblyDecompilerService` aufrufen → assert: Zielordner enthält mindestens eine `.csproj` und mindestens eine `.cs`.
- **Fehlerfall:** Nicht-Assembly-Pfad oder leerer Ordner → erwartete Exception / Fehlertext (falls öffentlich API).

### 2. `SolutionDiscoveryService`

- Tests aus Step 04 vervollständigen (falls dort nur skizziert): Parent-Name bei `…/X/decompile`.

### 3. `ConsoleOrchestrator`

- **Mock** `IAssemblyDecompilerService`: verifiziere, dass bei Assembly-Input `DecompileToProjectDirectory` mit erwartetem `targetDirectory` aufgerufen wird und `GetSolutionName`/`FindProjects` mit dem **Decompile-Pfad** erfolgen (Strict-Mock-Reihenfolge oder Callback-Assertions).
- Verzeichnis-Input: Decompiler wird **nicht** aufgerufen.

### 4. CLI

- Kurzer Test: Help/Usage-Strings enthalten Hinweis auf Assembly (Substring-Assert).

### 5. Bestehende Tests reparieren

- Alle Stellen mit `new ConsoleOrchestrator(...)` an den **neuen Konstruktor** anpassen (Moq `IAssemblyDecompilerService`).

## Ausführung

```bash
dotnet test SourceToAI.Tests/SourceToAI.Tests.csproj --configuration Release
```

(oder Solution-Datei, falls vorhanden)

## Qualitätsleiste

- Keine `#pragma warning disable` ohne Begründung.
- Keine fest eingebetteten riesigen DLLs — bevorzugt dynamische Erzeugung.

## Abhaken (Pflicht am Step-Ende)

- [ ] **Step 06 abgehackt** → `- [X] **Step 06 abgehackt**`
