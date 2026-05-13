# Step 02 — AssemblyDecompilerService + Interface + DI

## Ziel

Einen wiederverwendbaren Dienst bereitstellen, der eine **.dll** oder **.exe** per `WholeProjectDecompiler` in ein Zielverzeichnis decompiliert und den **Pfad zum Verzeichnis der erzeugten `.csproj`** zurückgibt (damit `FileDiscoveryService` / Loader unverändert bleiben).

## Kontext

- Konzept: [konzept.md](./konzept.md) (Ordner `…/{AssemblyName}/decompile/`, nicht löschen, vor erneutem Lauf Zielordner leeren).
- Projektrichtlinien: Interface nur, weil Orchestrierung + Testbarkeit — **`IAssemblyDecompilerService`** ist explizit gewollt.
- Ordner-Vorschlag: `SourceToAI.CLI/Services/Decompilation/` (neu) **oder** `Services/Processing/` — einheitlich wählen, Namespace `SourceToAI.CLI.Services.Decompilation` empfohlen.

## Aufgaben

### 1. Interface

Datei z. B. `IAssemblyDecompilerService.cs`:

- Methode z. B. `string DecompileToProjectDirectory(string assemblyFilePath, string targetDirectory, CancellationToken cancellationToken = default)`  
  - **assemblyFilePath:** absolute oder relative Pfadangabe zu `.dll`/`.exe` (Existenz + Endung vorher in Orchestrator/CLI prüfen — hier defensive `File.Exists` + sinnvolle Exception/`SourceToAiValidationException` optional).
  - **targetDirectory:** z. B. `{exportPath}/{assemblyName}/decompile` — der Dienst **stellt sicher**, dass das Verzeichnis leer startet: wenn `targetDirectory` existiert, **rekursiv löschen** und neu anlegen (Konzept: keine Artefakte).
  - **Rückgabe:** Verzeichnis, in dem die **Haupt-`.csproj`** liegt (meist gleich `targetDirectory`; wenn die API Unterordner erzeugt, den tatsächlichen Pfad ermitteln — in der Praxis schreibt `WholeProjectDecompiler` typischerweise direkt unter `targetDirectory`).

### 2. Implementierung `AssemblyDecompilerService`

- **`UniversalAssemblyResolver`** (Namespace je nach Paketversion, üblich `ICSharpCode.Decompiler.Util`) konfigurieren:
  - Basis-Suchpfad: Verzeichnis der Eingabe-Assembly: `Path.GetDirectoryName(Path.GetFullPath(assemblyFilePath))` — **zwingend** per `AddSearchDirectory` o. Ä. (Konzept: Auflösung referenzierter Assemblies).
- **`DecompilerSettings`** sinnvoll setzen, mindestens aus dem Konzept:
  - `RemoveDeadCode = true`, `YieldReturn = true`, `AsyncAwait = true` (falls die Property-Namen in der installierten Version leicht abweichen, an die **tatsächliche** API anpassen).
  - Falls verfügbar und sinnvoll: Optionen, die fehlende Referenzen tolerieren (Konzept: z. B. nicht bei jeder fehlenden GAC-Assembly abbrechen — **nur setzen, wenn die Property existiert**; kein Raten mit `#pragma` ohne Not).
- **`WholeProjectDecompiler`** mit Settings + Resolver instanziieren, `MetadataFile` laden (**`PEFile`** o. ä. laut Paketdoku), `DecompileProject(..., targetDirectory, cancellationToken)` aufrufen.
- Nach erfolgreichem Lauf: **Validierung**, dass mindestens eine `*.csproj` unter `targetDirectory` (rekursiv oder top-level — je nach ILSpy-Ausgabe) existiert; sonst klare Exception mit deutscher Meldung.
- **Kein** blockierendes I/O innerhalb unnötiger Locks; keine doppelte Assembly-Lese-Logik über die gesamte Pipeline hinaus (nur hier Decompiler-I/O).

### 3. Dependency Injection

In `SourceToAI.CLI/Program.cs`:

- `services.AddTransient<IAssemblyDecompilerService, AssemblyDecompilerService>();` (Lifetime konsistent zu anderen Transient-Services).

**Hinweis:** `ConsoleOrchestrator` wird erst in **Step 03** erweitert. Bis dahin existiert der Dienst im Container „ungenutzt“ — das ist akzeptabel, solange der Build grün ist.

## Tests in diesem Step

- Optional: Nur wenn ohne Step 03 sinnvoll — normalerweise reicht Step 06. Kein Zwang hier.

## Abhaken (Pflicht am Step-Ende)

- [X] **Step 02 abgehackt**
