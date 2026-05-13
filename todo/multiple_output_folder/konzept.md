# Plan zur Restrukturierung der SourceToAI Export-Pfade

## 🎯 Zielsetzung
Die bisherige Output-Struktur (`<Export-Pfad>/<Solution>/<View>/<Datei>`) wird umgebaut, um Dateien sowohl isoliert pro Solution als auch gemerged über alle Solutions hinweg bereitzustellen. Zudem erhalten alle Markdown-Dateien ein Suffix, das die Sicht (View) beschreibt.

**Neue Ziel-Struktur:**
```text
<Export-Pfad>/
 ├── .sta-marker                           <- Globaler Blocker/Marker (nicht mehr pro Solution)
 ├── readme.md                             <- Globale Dokumentation der Struktur
 ├── Isolated/
 │    ├── <SolutionA>/
 │    │    ├── dependency-graph.md
 │    │    ├── complete/
 │    │    │    └── <SolA>.<Proj1>-complete.md
 │    │    └── dto-only/
 │    │         └── <SolA>.<Proj1>-dto-only.md
 │    └── <SolutionB>/...
 └── Merged/
      ├── complete/
      │    ├── <SolA>.<Proj1>-complete.md
      │    └── <SolB>.<ProjX>-complete.md
      └── dto-only/
           ├── <SolA>.<Proj1>-dto-only.md
           └── <SolB>.<ProjX>-dto-only.md

```

## 🛠️ Umsetzungs-Schritte (für Cursor)

Bitte führe die folgenden Refactorings Schritt für Schritt durch und achte auf die Projektrichtlinien (kein I/O in Locks, keine neuen DI-Abstraktionen für Standard-I/O).

### 1. `Services\Export\MultiViewExportPaths.cs` anpassen

* **Neue Konstanten hinzufügen:** `IsolatedFolderName = "Isolated"` und `MergedFolderName = "Merged"`.
* **Dateinamen-Suffix integrieren:** Die Methode `BuildSanitizedExportFileStem` muss den `viewKey` als Parameter erhalten und anhängen.
* *Beispiel:* `return EnsureNotReservedWindowsStem($"{sol}.{proj}-{viewKey}");`


* **Pfade anpassen:** Die Methoden zur Pfadgenerierung müssen so umgebaut werden, dass sie entweder den Pfad für `Isolated` oder `Merged` zurückgeben können.
* `GetSolutionExportRoot` ist jetzt potenziell überholt oder muss `Isolated/<SolutionName>` abbilden.



### 2. `App\ConsoleOrchestrator.cs` anpassen (Orchestrierung & Marker)

* **Marker & Aufräumen globalisieren:** Die Methode `PrepareSolutionExportRootDirectory` darf nicht mehr innerhalb der Schleife `for (var i = 0; i < roots.Length; i++)` pro Solution aufgerufen werden. Sie muss **einmalig ganz am Anfang** (vor der Schleife) auf den globalen `exportPath` angewendet werden.
* Das bedeutet: Wenn das Tool läuft, wird der gesamte `exportPath` (z.B. `./out`) geprüft (ob `.sta-marker` existiert), geleert und neu angelegt.


* **Ordner anlegen:** Nach der Bereinigung direkt die Ordner `Path.Combine(exportPath, "Isolated")` und `Path.Combine(exportPath, "Merged")` anlegen.
* **Dependency Graph:** Dieser wird pro Solution nun unter `Path.Combine(exportPath, "Isolated", solutionName, "dependency-graph.md")` gespeichert.
* **Service Aufruf anpassen:** Beim Aufruf von `multiViewExportService.WriteMergedSolutionViews` wird als `outputRoot` nun der globale `exportPath` übergeben.

### 3. `Services\Export\MultiViewExportService.cs` anpassen

* **Signatur von WriteMergedSolutionViews prüfen:** Erhält nun den globalen `outputRoot`.
* **Zweifaches Schreiben (Isolated & Merged):**
* In der finalen Schleife, wo die zusammengesetzten `composedBodies` auf die Festplatte geschrieben werden (`WriteProjectViewFile`):
* Stammbildung anpassen: `MultiViewExportPaths.BuildSanitizedExportFileStem(solutionDisplayName, slot.Project.ProjectName, slot.ViewKey)` -> generiert z.B. `MySol.MyProj-complete`.
* Die Datei muss **zweimal** geschrieben werden:
1. In den `Isolated`-Pfad: `Path.Combine(outputRoot, "Isolated", solutionDisplayName, viewFolder, fileName)`
2. In den `Merged`-Pfad: `Path.Combine(outputRoot, "Merged", viewFolder, fileName)`


* *Wichtig:* Passe `WriteProjectViewFile` an, dass es beide Pfade bedienen kann oder rufe es einfach zweimal mit den vorberechneten Pfaden auf.



### 4. `Services\Export\MultiViewReadmeMarkdownGenerator.cs` anpassen

* Die Dokumentation (`readme.md`), die nun ins globale Root-Verzeichnis (`<Export-Pfad>/readme.md`) gelegt wird (durch den Orchestrator), muss aktualisiert werden.
* Sie muss die neuen Verzeichnisse `Isolated/` und `Merged/` erklären und darauf hinweisen, dass das `-<view>` Suffix an den Dateien existiert.
* Textbaustein-Idee für die Readme:
* `Merged/`: "Sammelt alle Projekte aller Solutions sortiert nach View. Perfekt, um z.B. alle DTOs oder alle Interfaces des gesamten Workspaces auf einmal in die KI zu laden."
* `Isolated/`: "Gruppiert die Exporte klassisch pro Solution."



### 5. Tests anpassen (WICHTIG!)

Da sich die Dateipfade und Suffixe stark geändert haben, werden viele Tests fehlschlagen.

* **Suche nach `.md` Assertions in:**
* `App\AiFeedProjectGranularityIntegrationTests.cs`
* `App\ConsoleOrchestratorTests.cs`
* `App\MultiViewExportIntegrationTests.cs`
* `App\MultiViewExportParallelDeterminismTests.cs`
* `Export\MultiViewExportPathsTests.cs`


* Passe die Assertions auf das neue Format an (z.B. `Path.Combine(outRoot, "Isolated", "GranSol", "complete", "GranSol.ProjA-complete.md")` und `Path.Combine(outRoot, "Merged", "complete", "GranSol.ProjA-complete.md")`).
* Stelle sicher, dass die Marker-Tests (`ConsoleOrchestratorTests.cs`) nun auf den Root-Exportordner prüfen und nicht auf den Solution-Ordner.

---

**Hinweis an die KI:**
Beginne mit `MultiViewExportPaths.cs` und dem `ConsoleOrchestrator.cs`. Führe danach die Änderungen in `MultiViewExportService.cs` durch und fixiere abschließend die Test-Suite. Gib am Ende eine Zusammenfassung der geänderten Dateien aus und hänge die passende `git commit` Message (gemäß Projektrichtlinien) an.

```
