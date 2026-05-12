**1. Architektonische Einordnung**

Das Design von SourceToAI weist eine klare funktionale Trennung in Discovery, Verarbeitung und Export auf. Allerdings leidet die Architektur unter klassischem "Enterprise-Bloat": Simple lokale Datei- oder Hash-Operationen werden durch Interfaces (`IFileReader`, `IHashService`) unnötig abstrahiert und über einen Dependency-Injection-Container orchestriert. Dies bringt für ein dediziertes CLI-Tool keinen funktionalen Mehrwert, erschwert die Lesbarkeit und verletzt den gewünschten YAGNI-Ansatz.

Zusätzlich konterkariert die fehlerhafte Implementierung des `Parse Once, Rewrite Multiple`-Prinzips die Bemühungen um Performance. Die Parallelisierung über `SemaphoreSlim` im Export-Service wird durch fundamentale Threading-Fehler im Document-Loader vollständig blockiert.

**2. BLOCKER (Kritische Fehler)**

* **Doppeltes Parsen (Redundanz):**
Die Methode `CSharpTransformedHasExportableSurface` in `AiFeedSegmentExportability` parst den bereits von den Rewritern umgeschriebenen und als String exportierten Code (`transformedText`) *erneut* in einen SyntaxTree, um zu prüfen, ob der Inhalt leer ist. Jede C#-Datei wird in den Views `signatures-only`, `public-only` und `dto-only` somit ein zweites Mal geparst.


* **Absturz bei gelockten Dateien im Load-Prozess:**
Während der `FileDiscoveryService` Zugriffsfehler (z.B. `UnauthorizedAccessException`) isoliert als Warnung behandelt, führt ein I/O-Fehler beim tatsächlichen Einlesen im `CSharpDocumentLoader` zum `ExtractionResult.Failure` für das gesamte Projekt. Eine einzelne, vom OS gesperrte `.cs`-Datei bricht den kompletten View-Build für dieses Projekt ab.


* **DI Anti-Pattern im Konstruktor:**
Die `MarkdownProjectViewBuilderBase` injiziert eine `IEnumerable<IViewGenerator>` und sucht sich im Konstruktor via `.Single(g => g.ViewKey == viewKey)` die passende Implementierung heraus. Jede Instanziierung iteriert über die Liste. Bei lokal begrenzten View-Buildern ist das eine unnötige Laufzeitbindung.



**3. Refactoring-Vorschläge**

**A. Lock-Freies Parsen im CSharpDocumentLoader**

Entferne das harte `lock` und nutze ein `ConcurrentDictionary` mit `Lazy<T>`, um sicherzustellen, dass jede Datei nur einmal gelesen/geparst wird, ohne Threads zu blockieren.

```csharp
// In CSharpDocumentLoader.cs
private readonly ConcurrentDictionary<string, Lazy<CachedCSharpParse>> _parseCache = new(StringComparer.OrdinalIgnoreCase);

public void Clear() => _parseCache.Clear();

public ExtractionResult<IReadOnlyList<ParsedCSharpDocument>> LoadParsedDocuments(...)
{
    // ...
    foreach (var path in absoluteFilePathsInDisplayOrder)
    {
        // ...
        var lazyParse = _parseCache.GetOrAdd(
            fullPath, 
            fp => new Lazy<CachedCSharpParse>(() => ReadAndParse(fp), LazyThreadSafetyMode.ExecutionAndPublication));
        
        try 
        {
            var cached = lazyParse.Value; // Hier geschieht I/O und Parsing threadsicher und parallel
            // ...
        }
        catch(Exception ex)
        {
            // Datei überspringen, statt ganzes Projekt abbrechen
            continue; 
        }
    }
    // ...
}

```

**B. Doppel-Parsing durch AST-Auswertung verhindern**

Der erneute Aufruf von `CSharpSyntaxTree.ParseText` in `AiFeedSegmentExportability` muss entfernt werden. Die Validierung, ob eine View leer ist, muss auf AST-Ebene direkt im `IViewGenerator` bzw. im `CSharpSyntaxRewriter` stattfinden.

Erweitere die `IViewGenerator`-Rückgabe:

```csharp
public record ViewGenerationResult(string OutputText, bool HasExportableSurface);

// In DtoRewriter / VisibilityRewriter etc. prüfen VOR dem ToFullString():
var rewrittenRoot = DtoRewriter.Rewrite(root);
bool hasSurface = rewrittenRoot.DescendantNodes().Any(n => n is BaseTypeDeclarationSyntax or EnumDeclarationSyntax ...);
return new ViewGenerationResult(rewrittenRoot.ToFullString(), hasSurface);

```

Dadurch entfällt der teure zweite Parse-Vorgang für tausende Dateien komplett.

**C. Enterprise-Bloat entfernen**


Zudem sollte der DI-Container für die View-Generatoren gestrafft werden. Statt `IEnumerable<IViewGenerator>` zu injizieren, sollten spezifische Fabriken genutzt oder die Zuordnung von Generator zu View-Key direkt als Delegate im Setup registriert werden.
