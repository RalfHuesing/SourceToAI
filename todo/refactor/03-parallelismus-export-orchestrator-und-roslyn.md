# Task 03 (optional): Parallelismus — Orchestrator, Export, Roslyn

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Nach `01` (gemeinsamer Parse-Cache) messen, ob noch CPU-bound Engpässe bestehen (`konzept.md` Abschnitt 3).
- Falls ja: begrenzte Parallelität über Projekte oder klar abgegrenzte Arbeitspakete einführen — **Projektrichtlinien:** `SemaphoreSlim` (z. B. Obergrenze 5), Fehler in `ConcurrentQueue<Exception>`, am Ende ggf. `AggregateException`.
- Determinismus der Ausgabe (Sortierung der Projekte, stabile Reihenfolge in Manifesten) darf nicht leiden.

## Nicht-Ziel

- Kein „maximaler“ Parallelismus ohne Konzept.
- Keine Änderung der exportierten Markdown-Struktur.

## Abhängigkeiten

- **Task `01` abgeschlossen**, damit nicht parallel dieselben Dateien redundant geparst werden (sonst verschlechtert Parallelität nur die Symptome).

## Tests (Pflicht, wenn umgesetzt)

- Bestehende Integrationstests müssen grün bleiben.
- Wo möglich: Test mit künstlicher Verzögerung oder mehreren kleinen Projekten, der zeigt, dass keine Race-Conditions die Dateiliste korrumpieren.

## Selbstverifikation (nach Umsetzung)

- [x] Begründung dokumentiert (warum Parallelität, wo gemessen).
- [x] `dotnet test` grün; keine neuen Warnungen.
- [x] `00-epic-master-checklist-selbstverifikation.md`: Matrix-Zeile „Parallelisierung“ abhaken oder explizit als „bewusst verschoben“ markieren.

## Umsetzung (Stand)

- **Begründung:** `konzept.md` Abschnitt 3 — sequentieller Engpass bei vielen Projekten/Views durch CPU-lastiges `BuildContentSegments` (Roslyn + Rewriter) und `Compose`. Kein separates Mikro-Benchmarking im Repo; Engpass qualitativ aus der Architektur (N×M sequentiell) abgeleitet.
- **Code:** `MultiViewExportService` — Arbeitspakete = feste Reihenfolge der bisherigen Schleife (Export-Units × `ViewKeyOrder`). **Parallel** (max. 5): `BuildContentSegments` + `Compose`. **Sequenziell danach:** `AllocateUniqueFileStem` + `File.WriteAllText`, damit `usedStemsPerView` und Ausgabepfade deterministisch bleiben. Unerwartete Exceptions → `ConcurrentQueue<Exception>` → `AggregateException` als `ExtractionResult`-Fehlertext.
- **Orchestrator:** unverändert sequentiell; I/O-Discovery parallel zu halten war nicht nötig für diese Task.
- **Tests:** `MultiViewExportParallelDeterminismTests` — fünf Mini-Projekte, Eingabe absichtlich permutiert, acht Exportläufe, bit-identischer Fingerabdruck der View-Bäume.

## Nächster Schritt

`04-yaml-escaping-zentralisieren.md` oder `05-signatures-rewriter-speicher-druck-optional.md`.
