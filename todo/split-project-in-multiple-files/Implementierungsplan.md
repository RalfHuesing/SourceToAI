# Implementierungsplan: Adaptives Größen-Clustering (Bin-Packing für Namespaces) in SourceToAI

Dieser Implementierungsplan beschreibt die detaillierte technische Umsetzung von **Konzept 2: Adaptives Größen-Clustering** zur automatischen, namespace-basierten Aufteilung großer .NET-Projekte in mehrere Markdown-Feeds. 

Das Ziel ist es, Markdown-Dateien vollautomatisch zu erzeugen, die sowohl eine gewünschte Dateigröße (`maxFileSize`) anstreben als auch eine **harte Obergrenze** für die Dateianzahl (`maxFileCount`) strikt einhalten.

---

## 1. Design-Ziele & Anforderungen

1. **Vollautomatisch (Konfigurationsfrei):** Der Benutzer muss keine Namespaces manuell angeben. Die Aufteilung erfolgt rein auf Basis einer statischen Code-Analyse.
2. **Globale Parameter:**
   * `maxFileSize` (in KB): Die gewünschte maximale Größe einer einzelnen Markdown-Datei (Richtwert/Soft-Limit).
   * `maxFileCount`: Die **harte** Obergrenze für die Anzahl der generierten Dateien pro realem C#-Projekt (z. B. maximal 10 Dateien wegen Upload-Limits bei Web-LLMs).
3. **Priorität der Grenzwerte:**
   * `maxFileCount` ist ein **hartes Limit** und darf unter keinen Umständen überschritten werden.
   * Wenn nötig, wird das Soft-Limit `maxFileSize` überschritten, um die maximale Dateianzahl einzuhalten.
4. **Aktivierung:** Das Splitting-Feature ist aktiv, wenn **beide** Werte (`maxFileSize` und `maxFileCount`) größer als `0` sind.
5. **CLI & Settings:** Die Konfiguration kann über die Kommandozeile (`--max-file-size` / `--max-file-count`) **oder** über die `appsettings.json` erfolgen. CLI-Parameter überschreiben Config-Werte.
6. **Konsistenz über alle Views:** Die Zuweisung einer Datei zu einem Bucket erfolgt einmalig und ist für alle Views (`complete`, `signatures-only`, `public-only`, `dto-only`) identisch. So enthalten die entsprechenden Dateien über alle Views hinweg exakt die gleichen Klassen.

---

## 2. Der intelligente Clustering-Algorithmus (Bottom-Up-Kollaps)

Ein einfaches Aufteilen jeder Datei in eine eigene Datei führt zu einer Zersplitterung. Unser Algorithmus nutzt die **C#-Namespace-Hierarchie als Baum** und führt einen gesteuerten bottom-up Zusammenschluss (Collapse) durch.

### Funktionsweise des Algorithmus:

```mermaid
graph TD
    A[1. Quellcode parsen & Namespaces extrahieren] --> B[2. Namespace-Baum erstellen]
    B --> C[3. Blätter als initiale Buckets setzen]
    C --> D{Anzahl Buckets > maxFileCount?}
    D -- Ja -- > E[4. Geschwister-Buckets mit kleinster Gesamtgröße mergen]
    E --> D
    D -- Nein --> F{Gibt es Buckets < Soft-Limit und passende Geschwister?}
    F -- Ja, und Parent bleibt <= maxFileSize --> G[5. Optionale Optimierung: Kleine Buckets mergen]
    G --> F
    F -- Nein --> H[6. Virtuelle Projekte & Dateinamen benennen]
```

### Die Phasen im Detail:

1. **Parser- & Größen-Erfassung:**
   * Alle `.cs`-Dateien des Projekts werden über den `ICSharpDocumentLoader` geladen und geparst.
   * Für jede Datei wird die originale Quelltext-Größe in Bytes ermittelt.
   * Der vollständige Namespace der Datei wird aus der Roslyn-Syntaxbaum-Wurzel (`CompilationUnitSyntax`) über `BaseNamespaceDeclarationSyntax` ausgelesen.
   * Nicht-C#-Dateien (z. B. `.json`, `.sql`, `.csproj`) erhalten einen virtuellen Namespace wie `[Assets]`.

2. **Aufbau des Namespace-Baums:**
   * Es wird ein Baum konstruiert, bei dem jeder Knoten ein Namespace-Segment repräsentiert.
   * *Beispiel:* `San.smart.Planner.Platform.Features.Bookings` führt zu den Knoten: `San` -> `smart` -> `Planner` -> `Platform` -> `Features` -> `Bookings`.
   * Jeder Knoten speichert die Liste der in ihm enthaltenen Dateien und die Summe der Dateigrößen (inklusive aller Kind-Knoten).

3. **Initiale Buckets:**
   * Jeder Blattknoten des Baums, der Dateien enthält, startet als separater Bucket.

4. **Bottom-Up Merge (Harte Grenze erzwingen):**
   * Solange die Anzahl der Buckets größer als `maxFileCount` ist:
     * Suche im Baum nach einem Elternknoten $p$, dessen aktive Kinder alle in der Bucket-Liste enthalten sind.
     * Wähle den Elternknoten $p$ aus, bei dem die **Summe der Dateigrößen aller Kinder am kleinsten** ist.
     * Entferne die Kinder aus der Bucket-Liste und füge stattdessen das übergeordnete Element $p$ als neuen gemeinsamen Bucket hinzu.
     * Dies verringert die Anzahl der Buckets um $(\text{Anzahl Kinder} - 1)$.
     * *Hinweis:* Dieser Schritt läuft so lange, bis die harte Grenze `maxFileCount` erreicht oder unterschritten ist. Das Überschreiten von `maxFileSize` wird hierbei in Kauf genommen.

5. **Geschwister-Optimierung (Soft-Limit & Aufräumen kleiner Buckets):**
   * Wenn wir die harte Grenze eingehalten haben, können immer noch viele kleine, zersplitterte Buckets existieren (z. B. 8 Buckets mit je 5 KB).
   * Wir führen optionale Merges von Geschwister-Knoten durch, solange die resultierende Gesamtgröße des Elternknotens $\le \text{maxFileSize}$ bleibt, um eine saubere, kompakte Dateistruktur zu erhalten.

6. **Namensgebung der virtuellen Projekte:**
   * Jeder verbleibende Bucket wird als eigenständiges **virtuelles Projekt** exportiert.
   * Der Name des virtuellen Projekts leitet sich aus dem Namespace-Pfad des Buckets ab (z. B. `San.smart.Planner.Platform.Features` -> Name: `Platform.Features`).
   * Dateien im `[Assets]`-Bucket erhalten den Suffix `_Assets` (z. B. `San.smart.Planner.Platform._Assets`).

---

## 3. Technische Umsetzungsschritte

### Schritt 1: Konfiguration & CLI-Binding
1. **Erweiterung von `AppSettings.cs`:**
   ```csharp
   public class AppSettings
   {
       // ... Bisherige Werte ...
       
       public int MaxFileSizeKb { get; set; } = 0;
       public int MaxFileCount { get; set; } = 0;
   }
   ```
2. **Anpassung der CLI-Argumente in `SourceToAiCli.cs`:**
   * Registrierung von zwei neuen globalen Optionen:
     * `--max-file-size <kb>`
     * `--max-file-count <anzahl>`
   * Mappen der CLI-Werte auf die instanziierte `AppSettings` in `Program.cs`. Wenn CLI-Optionen übergeben wurden, überschreiben diese die Werte aus der `appsettings.json`.

### Schritt 2: Entwicklung des Namespace-Extraktors
Wir fügen eine Utility-Methode hinzu, um den Namespace einer C#-Datei effizient aus der Roslyn `CompilationUnitSyntax` auszulesen:
```csharp
public static string GetNamespace(CompilationUnitSyntax root)
{
    var namespaceDecl = root.Members.OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
    return namespaceDecl?.Name.ToString() ?? string.Empty;
}
```
*Da Roslyn-Syntaxbäume im `ICSharpDocumentLoader` bereits materialisiert und im Arbeitsspeicher gecached sind, entstehen durch diesen Scan keinerlei zusätzliche I/O- oder Parse-Kosten.*

### Schritt 3: Der Partitionierungs-Service
Erstellung einer neuen Klasse `ProjectSplittingEngine` (z. B. unter `Services/Processing/Discovery/`), die die Splitting-Logik kapselt:
```csharp
public sealed class ProjectSplittingEngine(ICSharpDocumentLoader csharpDocumentLoader)
{
    public IReadOnlyList<VirtualProjectPartition> PartitionProject(
        ProjectDefinition project,
        IReadOnlyList<string> absoluteFilePaths,
        int maxFileSizeKb,
        int maxFileCount)
    {
        // 1. Lade Dokumente über csharpDocumentLoader (nutzt den Cache!)
        // 2. Erstelle Namespace-Baum
        // 3. Führe Bottom-Up-Kollaps aus
        // 4. Erzeuge VirtualProjectPartition-Objekte (mit angepasstem Projektnamen und Pfad-Teillisten)
    }
}
```

### Schritt 4: Pipeline-Integration in `MultiViewExportService`
Wir integrieren das Splitting direkt am Anfang der Pipeline in `MultiViewExportService.WriteMergedSolutionViews`. 

Dadurch wird aus einem realen Projekt eine Liste von virtuellen Projekten erzeugt. Der gesamte nachfolgende Prozess (Generierung der Views, Generierung der Manifeste, Schreiben der Dateien) bleibt **unverändert**:

```csharp
// In MultiViewExportService.cs

public void WriteMergedSolutionViews(...)
{
    csharpDocumentLoader.Clear();
    
    // ... Initialisierung ...

    var exportUnits = new List<(ProjectDefinition Project, IReadOnlyList<string> Paths, bool DocsOnlyInCompleteView)>();

    // Vorbereitung des Splitting-Engine-Aufrufs
    var isSplittingActive = appSettings.MaxFileSizeKb > 0 && appSettings.MaxFileCount > 0;

    foreach (var (project, paths) in orderedProjects)
    {
        if (paths.Count == 0) continue;

        if (isSplittingActive)
        {
            // Führe das adaptive Splitting durch
            var partitions = splittingEngine.PartitionProject(project, paths, appSettings.MaxFileSizeKb, appSettings.MaxFileCount);
            
            foreach (var partition in partitions)
            {
                // Erstelle ein virtuelles ProjectDefinition-Objekt für jede Partition
                var virtualProjName = $"{project.ProjectName}.{partition.SubNamespaceName}";
                var virtualCsproj = Path.Combine(Path.GetDirectoryName(project.CsprojPath)!, $"{virtualProjName}.virtual.csproj");
                
                var virtualProject = new ProjectDefinition(virtualProjName, virtualCsproj);
                exportUnits.Add((virtualProject, partition.Paths, false));
            }
        }
        else
        {
            exportUnits.Add((project, paths, false));
        }
    }
    
    // Der restliche Ablauf (RunBoundedParallel, View-Builds, Markdown-Composer, Schreiben)
    // arbeitet nahtlos mit den virtuellen Projekten weiter!
}
```

---

## 4. Entscheidungsfragen & Abstimmung (User Review)

Um die Implementierung exakt auf deine Bedürfnisse abzustimmen, beantworte bitte die folgenden Fragen. Kopiere einfach die Checkboxen in deine Antwort und markiere deine Wahl mit einem `[x]`.

### Frage 1: Umgang mit Nicht-C#-Dateien (Assets / wwwroot / configs)
Nicht-C#-Dateien besitzen keine echten C#-Namespaces. Wie sollen diese im Clustering behandelt werden?

* **[x] Option A (Empfohlen):** Alle Nicht-C#-Dateien in einen separaten Asset-Bucket packen (z. B. `[ProjektName]._Assets-complete.md`). Dies hält den Code sauber getrennt von Konfigurationsdateien und statischen Web-Assets.
* **[ ] Option B:** Ordnerstrukturen von Nicht-C#-Dateien als virtuelle Namespaces interpretieren (z. B. `wwwroot/js/site.js` -> Namespace `wwwroot.js`) und diese ganz normal im Baum mit-clustern.
* **[ ] Option C:** Alle Nicht-C#-Dateien stumpf in den ersten generierten Code-Bucket hineinwerfen.

---

### Frage 2: Behandlung von globalem Code / Top-Level Code
Einige C#-Klassen liegen direkt im globalen Namespace (ohne `namespace`-Deklaration) oder in sehr kurzen Namespaces (z. B. nur `Program.cs` im Root). Wie sollen diese gruppiert werden?

* **[x] Option A (Empfohlen):** In einen standardmäßigen Root/Core-Bucket packen (z. B. `[ProjektName].Core-complete.md`), der alle Dateien ohne Namespace sowie die verbleibenden Systemdateien aufnimmt.
* **[ ] Option B:** Als eigenständigen Namespace `[Global]` behandeln, der als separates Blatt im Baum startet und gegebenenfalls durch den Algorithmus mit anderen Clustern verschmolzen wird.

---

### Frage 3: Detaillierungsebene der virtuellen Projektnamen
Wenn Namespaces zusammengelegt werden (z. B. `Platform.Features.Bookings` und `Platform.Features.Billing` zu `Platform.Features`), wie detailliert soll der Name der erzeugten Markdown-Dateien sein?

* **[x] Option A (Empfohlen):** Immer den gemeinsamen Namespace-Knotenpunkt verwenden, an dem der Zusammenfluss stattfand (z. B. `Platform.Features`). Dies erzeugt sehr saubere, verständliche Dateinamen wie `Platform.Features-complete.md`.
* **[ ] Option B:** Die Namen aller enthaltenen Blätter verketten (z. B. `Platform.Features.Bookings_Billing`). Dies ist präziser, kann aber bei vielen kleinen Namespaces zu extrem langen Dateinamen führen.

---

## 5. Verifikationsplan

Nach der Implementierung wird die Funktionalität wie folgt abgesichert:

### Automatisierte Unit-Tests
In `SourceToAI.Tests` werden neue Tests implementiert:
1. **`ProjectSplittingEngineTests`**:
   * Test des Bottom-up-Baumaufbaus mit Scheindokumenten unterschiedlicher Größe.
   * Validierung, dass das harte Limit `maxFileCount` niemals überschritten wird (z. B. 15 Namespaces auf 3 Buckets reduzieren).
   * Validierung, dass die Soft-Grenze `maxFileSize` überschritten wird, wenn die Anzahl der Namespaces das harte Limit übersteigt.
2. **`CommandLineIntegrationTests`**:
   * Überprüfung der Parameter-Übergabe `--max-file-size` und `--max-file-count` an den Orchestrator.

### Manuelle Verifikation
* Export eines realen, großen Testprojekts (wie das genannte `San.smart.Planner.Platform`) mit aktivierten Parametern:
  `SourceToAI.exe C:\Export C:\Planner --max-file-size 400 --max-file-count 8`
* Prüfung der generierten Dateien im Export-Ordner `Merged/complete/` auf korrekte Namensgebung, Einhaltung der Dateigrößen und Einhaltung der maximalen Dateianzahl.
