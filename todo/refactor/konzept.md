# SourceToAI - Code Audit & Refactoring Plan

## 1. 🔴 KRITISCHER BUG: Absturz auf macOS/Linux (Fehlende `appsettings.json`)
**Problem:** In `Program.cs` wird die `appsettings.json` als zwingend erforderlich deklariert (`optional: false`). In der Datei `.github\workflows\release.yml` wird diese Datei beim Publish aber *nur* für `windows-latest` mitkopiert (`${{ matrix.os == 'windows-latest' && './publish/appsettings.json' || '' }}`). Wenn ein macOS- oder Linux-User das Tool startet, crasht es sofort mit einer `FileNotFoundException`. 
Zusätzlich: Wenn die Datei fehlen darf, knallt es im `FileDiscoveryService`, weil die Arrays in `AppSettings.cs` standardmäßig leer sind (`[]`) und somit 0 Dateien gefunden werden.

**To-Do:**
1. **`Configuration\AppSettings.cs`**:
   Füge die Fallback-Standardwerte direkt in die Properties ein, damit das Tool auch komplett ohne JSON-Datei Out-of-the-Box funktioniert.
   ```csharp
   public class AppSettings
   {
       public string[] ExcludedDirectories { get; set; } = [ "bin", "obj", ".git", ".vs", ".idea", "node_modules" ];
       public string[] IncludedExtensions { get; set; } = [ ".cs", ".sql", ".json", ".xml", ".xaml", ".yml", ".md", ".mdc", ".js", ".ts", ".css", ".csproj" ];
   }

```

2. **`Program.cs`**:
Mache die Datei optional, um den Crash zu verhindern.
```csharp
// Vorher:
.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
// Nachher:
.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)

```



---

## 2. 🟡 PERFORMANCE / BOILERPLATE: `FileDiscoveryService` String-Allokationen

**Problem:**
Im `FileDiscoveryService.cs` (`ScanDirectory` und `FindSolutionDocs`) wird für jede Datei `Path.GetExtension(file).ToLowerInvariant()` aufgerufen und dann in einem Array mittels `.Contains()` (lineare Suche) gesucht. Das erzeugt unnötige String-Allokationen und kostet Laufzeit bei großen Repositories.

**To-Do:**

1. **`Services\Discovery\FileDiscoveryService.cs`**:
Konvertiere die Arrays aus `settings` idealerweise *einmalig* im Konstruktor oder zu Beginn der Methode in ein `HashSet<string>(StringComparer.OrdinalIgnoreCase)`.
```csharp
// Beispiel für den Service:
var includedExts = new HashSet<string>(settings.IncludedExtensions, StringComparer.OrdinalIgnoreCase);
var excludedDirs = new HashSet<string>(settings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);

```


2. Entferne die `.ToLowerInvariant()`-Aufrufe bei den Dateiprüfungen und nutze das O(1) Lookup der HashSets.

---

## 3. 🟡 PERFORMANCE / THREADPOOL: Suboptimale Semaphore-Nutzung

**Problem:**
In `MultiViewExportService.cs` (Methode `RunBoundedParallel`) wird für jede Aufgabe sofort ein ThreadPool-Thread per `Task.Run()` angefordert, der dann *innerhalb* des Tasks auf das `SemaphoreSlim` wartet (`semaphore.Wait()`). Wenn es 500 Projekte mit je 4 Views gibt, feuern wir sofort 2000 Tasks in den ThreadPool, was zu ThreadPool-Starvation führen kann.

**To-Do:**

1. **`Services\Export\MultiViewExportService.cs`**:
Passe `RunBoundedParallel` an. Entweder modern per `Parallel.ForEachAsync` (da wir .NET 8/10 nutzen) oder verschiebe das `WaitAsync()` vor das `Task.Run()`.
*Empfohlene Variante (`Parallel.ForEachAsync` spart den ganzen Semaphore-Boilerplate):*
```csharp
private static void RunBoundedParallel(int maxConcurrency, int workCount, Action<int> work, ConcurrentQueue<Exception> errors)
{
    if (workCount <= 0) return;

    var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(maxConcurrency, 1, int.MaxValue) };

    Parallel.For(0, workCount, options, i =>
    {
        try
        {
            work(i);
        }
        catch (Exception ex)
        {
            errors.Enqueue(ex);
        }
    });
}

```


*(Hinweis: Da die inneren Arbeiten via `work(i)` synchrone Roslyn-Aufgaben sind, passt ein normales `Parallel.For` hier perfekt).*

