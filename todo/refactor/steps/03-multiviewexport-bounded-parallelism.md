# Schritt 03: `MultiViewExportService` — begrenzter Parallelismus ohne ThreadPool-Spam

## Ziel

`RunBoundedParallel` soll **nicht** pro Arbeitseinheit sofort `Task.Run` starten, dessen Thread dann blockierend auf ein `SemaphoreSlim` wartet. Stattdessen: Parallelität direkt über die eingebaute Threadpool-Steuerung mit **oberer Grenze** (`MaxDegreeOfParallelism`), wie im Konzept mit `Parallel.For` skizziert.

## Betroffene Datei

- `SourceToAI.CLI/Services/Export/MultiViewExportService.cs`  
  Private Methode `RunBoundedParallel` (aktuell: `SemaphoreSlim` + `Task[]` + `Task.WaitAll`).

## Umsetzung

### 3.1 Implementierung

- `workCount <= 0` → wie bisher sofort return.
- `ParallelOptions` mit  
  `MaxDegreeOfParallelism = Math.Clamp(maxConcurrency, 1, int.MaxValue)`.
- `Parallel.For(0, workCount, options, i => { … })` im `try/catch` pro Iteration: Exceptions in `errors` enqueuen (gleiches Fehleraggregations-Muster wie heute).
- `SemaphoreSlim`, `Task`-Array und `Task.WaitAll` **entfernen**, sofern nirgends sonst benötigt.

### 3.2 Annahme prüfen (kurz im Code verifizieren)

- Die übergebene `work`-Delegate-Arbeit ist **synchron** und CPU-/Roslyn-lastig (kein `async` in der Callback-Kette). Das passt zu `Parallel.For`. Wenn innerhalb von `work(i)` dennoch async-APIs ohne Blockade genutzt werden, kurz prüfen — bei rein synchronem Roslyn-Export ist `Parallel.For` passend (wie Konzept-Hinweis).

### 3.3 Aufrufer

- Aufrufstelle von `RunBoundedParallel` unverändert lassen, soweit die Signatur `(maxConcurrency, workCount, work, errors)` bestehen bleibt.

## Tests

- `dotnet test` — insbesondere `SourceToAI.Tests/App/MultiViewExportIntegrationTests.cs`, sofern Parallelität und Fehlersammlung dort abgedeckt sind.
- Keine neuen Warnungen (Projektrichtlinie: behebbare Warnungen nicht stehen lassen).

## Akzeptanzkriterien

- [ ] Bei vielen Arbeitseinheiten entstehen **nicht** `workCount` viele gleichzeitig gestartete `Task.Run`-Jobs, die primär auf ein Semaphore warten.
- [ ] Maximaler Parallelitätsgrad entspricht weiterhin `maxConcurrency` (geclamped).
- [ ] Fehler werden weiterhin in `ConcurrentQueue<Exception>` gesammelt; nachgelagerte Logik (z. B. `AggregateException`) bleibt konsistent.
- [ ] `dotnet test` grün.

## Nicht-Ziel

- Kein Wechsel auf `Parallel.ForEachAsync`, solange die Arbeitsschicht synchron bleibt (optional später bei echter Async-I/O-Schicht).

## Abschluss Refactor-Reihe

Nach diesem Schritt sind die drei Punkte aus `todo/refactor/konzept.md` umgesetzt. Optional: Konzept-Datei um Status-Checkboxen ergänzen oder auf diese Step-Dateien verweisen — nur wenn das Team das pflegen möchte.
