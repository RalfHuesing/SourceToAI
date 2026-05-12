### 1. Architektonische Einordnung

Das grundlegende Design der Applikation ist solide und entspricht einem pragmatischen, modularen CLI-Ansatz. Die "Matrix-Struktur" wurde erfolgreich implementiert: Die Trennung in Sichten (`complete`, `signatures-only`, etc.) und Projekte (`Solution.Project.md`) ist über die `MultiViewExportPaths` und den `MultiViewExportService` klar definiert. Es entsteht kein Monolith. Die Anforderung an Manifest und Frontmatter ist über den `AiFeedMarkdownComposer` vollständig und typsicher umgesetzt. Die `Program.cs` ist durch die Nutzung der Extension-Methods `AddViewGenerators()` und `AddMarkdownProjectViewBuilders()` schlank geblieben. Das Dependency Injection Setup sorgt für eine saubere Separation of Concerns.

### 2. Blocker (Kritische Fehler & verfehlte Anforderungen)

* **Verfehlte Anforderung: Parse Once, Rewrite Multiple Times.**
Der Code liest und parst C#-Dateien massiv redundant. Im `MultiViewExportService` wird synchron über die Sichten (`ViewKeyOrder`) iteriert. Für jede Sicht wird `builder.BuildContentSegments()` aufgerufen, was intern jedes Mal `csharpDocumentLoader.LoadParsedDocuments()` triggert. Da der `CSharpDocumentLoader` seinen Status (`seenFullPaths`) nur methodenlokal hält, wird **jede C#-Datei für jede konfigurierte View (4x) neu von der Festplatte gelesen und durch Roslyn geparst**. Bei hohem Token-Volumen führt dies zu einer massiven Laufzeitverschlechterung durch redundanten Disk-I/O und CPU-Overhead.


* **Abbruch kompletter Projekte bei Single-File-Exceptions.**
Die rekursive Methode `ScanDirectory` im `FileDiscoveryService` wird von einem globalen `try-catch` umschlossen. Wenn nur eine einzige Datei in einem Unterordner durch fehlende Berechtigungen (z. B. `UnauthorizedAccessException`) gesperrt ist, bricht der gesamte Projekt-Scan ab und das Projekt wird komplett ignoriert.



### 3. Roslyn & Performance

* **Fehlende Parallelisierung:** Der `ConsoleOrchestrator` und der `MultiViewExportService` arbeiten strikt sequenziell über eine `foreach`-Schleife. Bei stark genutzten Projekten (viele C#-Dateien) verschenkt das synchrone Parsing und Rewriting immenses Potenzial der CPU.


* **Roslyn Memory Allocation:** Der `SignaturesRewriter` ruft auf entfernten Bodies permanent `WithBody(null).WithExpressionBody(null)` auf. Dies generiert auf jedem Knoten neue Immutable-Trees. Dies ist sicher implementiert, erhöht aber den Speicherbedarf der CLI, was bei sehr großen Solutions in OutOfMemory-Szenarien enden kann, wenn keine Garbage Collection greift.


* **Dynamic Fencing Logik:** Die Methode `CalculateRequiredBackticks` im `MarkdownFenceUtility` ist robust und korrekt iterativ (O(n)) implementiert, um die Längen der Code-Blöcke dynamisch abzusichern.



### 4. Refactoring & Clean Code

* **Caching im `CSharpDocumentLoader` einführen:**
Um die "Parse Once"-Anforderung zu erfüllen, muss der Loader als Singleton oder per-Run-Scoped-Cache fungieren.
*Vorschlag zur Vereinfachung (`CSharpDocumentLoader.cs`):*
```csharp
public sealed class CSharpDocumentLoader(IFileReader fileReader) : ICSharpDocumentLoader
{
    // Cache pro absolutem Pfad
    private readonly Dictionary<string, ParsedCSharpDocument> _documentCache = new(StringComparer.OrdinalIgnoreCase);

    public ExtractionResult<IReadOnlyList<ParsedCSharpDocument>> LoadParsedDocuments(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePathsInDisplayOrder)
    {
        try
        {
            var documents = new List<ParsedCSharpDocument>();

            foreach (var path in absoluteFilePathsInDisplayOrder)
            {
                if (!string.Equals(Path.GetExtension(path), ".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                var fullPath = Path.GetFullPath(path);

                if (_documentCache.TryGetValue(fullPath, out var cachedDoc))
                {
                    documents.Add(cachedDoc);
                    continue;
                }

                var sourceText = fileReader.ReadAllText(fullPath);
                // ... Parsing Logik bleibt identisch ...

                var newDoc = new ParsedCSharpDocument(...);
                _documentCache[fullPath] = newDoc;
                documents.Add(newDoc);
            }
            return ExtractionResult<IReadOnlyList<ParsedCSharpDocument>>.Success(documents);
        }
        // ...
    }
}

```


* **Fehlertolerantes I/O-Scanning:**
Das Exception-Handling im `FileDiscoveryService` muss granulärer gestaltet werden (YAGNI auf Projektebene, aber zwingend erforderlich für Robustheit).
*Vorschlag (`FileDiscoveryService.cs`):*

```csharp
    private void ScanDirectory(string currentDir, List<string> foundFiles, AppSettings settings)
    {
        try 
        {
            foreach (var file in Directory.GetFiles(currentDir))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (settings.IncludedExtensions.Contains(ext))
                {
                    foundFiles.Add(file);
                }
            }
        } 
        catch (UnauthorizedAccessException) { /* Überspringen und weiterarbeiten */ }
        
        try
        {
            foreach (var dir in Directory.GetDirectories(currentDir))
            {
                var dirName = new DirectoryInfo(dir).Name;
                if (!settings.ExcludedDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                {
                    ScanDirectory(dir, foundFiles, settings);
                }
            }
        }
        catch (UnauthorizedAccessException) { /* Überspringen */ }
    }
    ```

*   **Redundanter Code bei StringBuilder-Logik:** In `AiFeedMarkdownComposer` und `MarkdownFeedGenerator`[cite: 3] wird YAML sehr kleinteilig per String-Interpolation gebaut. Dies ist pragmatisch, birgt aber bei künftigen Anpassungen Inkonsistenzen. Eine dedizierte YAML-Builder-Struktur wird aufgrund der YAGNI-Prinzipien noch nicht benötigt, aber die Methode `EscapeYamlDoubleQuoted` im Composer[cite: 3] sollte zentral ausgelagert werden.

```