# Schritt 02: `FileDiscoveryService` — HashSet & weniger Allokationen

## Ziel

Extension- und Verzeichnis-Filterung ohne pro-Datei-`ToLowerInvariant()`-Allokation und ohne lineare `Array.Contains`-Suche; Vergleiche **ordinal case-insensitive** über `HashSet<string>` mit `StringComparer.OrdinalIgnoreCase`.

## Voraussetzung

- Schritt **01** ist erledigt (sinnvolle Defaults in `AppSettings`); dieser Schritt ist davon unabhängig lauffähig, aber die Kombination aus leeren Settings + diesem Refactor sollte nicht mehr im Alltag vorkommen.

## Betroffene Datei

- `SourceToAI.CLI/Services/Discovery/FileDiscoveryService.cs`

## Ist-Analyse (Stand Konzept)

- `FindSolutionDocs`: `.Where(f => settings.IncludedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))` — Allokation + O(n) pro Datei.
- `ScanDirectory` (in `FindFilesForProject`): gleiches Muster für Extensions; `ExcludedDirectories` nutzt bereits `Contains(..., StringComparer.OrdinalIgnoreCase)` — kann auf ein `HashSet` mit demselben Comparer vereinheitlicht werden.

## Umsetzung

### 2.1 HashSets pro Aufruf bauen (oder einmal pro Top-Level-Methode)

- Zu Beginn von `FindSolutionDocs`:  
  `var included = new HashSet<string>(settings.IncludedExtensions, StringComparer.OrdinalIgnoreCase);`  
  Filter: `included.Contains(Path.GetExtension(f))` — **ohne** `ToLowerInvariant()`.

- Zu Beginn von `FindFilesForProject` (vor `ScanDirectory`):  
  dieselben `included`-Sets erzeugen; zusätzlich  
  `var excluded = new HashSet<string>(settings.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);`

### 2.2 Signatur `ScanDirectory`

- `ScanDirectory` erhält die vorgebauten `HashSet`-Instanzen als Parameter (statt jedes Mal aus `AppSettings` zu lesen), damit rekursive Aufrufe keine wiederholte Konvertierung machen.

### 2.3 Verzeichnisnamen

- Statt `settings.ExcludedDirectories.Contains(dirName, StringComparer.OrdinalIgnoreCase)` → `excluded.Contains(dirName)`.

## Tests

- `SourceToAI.Tests/Discovery/FileDiscoveryServiceTests.cs`: bestehende Szenarien müssen grün bleiben; bei Bedarf **einen** zusätzlichen Test mit gemischter Groß/Kleinschreibung der Extension (z. B. `.CS` vs `.cs`), um das Ignore-Case-Verhalten abzusichern — nur wenn kein bestehender Test das schon abdeckt.
- `dotnet test`.

## Akzeptanzkriterien

- [ ] Keine `ToLowerInvariant()`-Nutzung für Extension-Matching in diesem Service (außer es gibt eine dokumentierte Ausnahme — sollte nicht nötig sein).
- [ ] Keine `settings.IncludedExtensions.Contains(...)` / lineare Suche in heißen Pfaden.
- [ ] Verhalten für leere `IncludedExtensions`/`ExcludedDirectories` Arrays bleibt definiert (nach Schritt 01 typischerweise nicht mehr leer bei Default-Instanz; explizit leer konfiguriert = weiterhin respektieren).

## Nicht-Ziel

- Kein Wechsel des öffentlichen Contracts von `IFileDiscoveryService`.
- Kein zusätzliches DI/Interface nur für diese Optimierung.

## Referenz für den nächsten Agenten

- Anschließend: `03-multiviewexport-bounded-parallelism.md`.
