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

- [ ] Begründung dokumentiert (warum Parallelität, wo gemessen).
- [ ] `dotnet test` grün; keine neuen Warnungen.
- [ ] `00-epic-master-checklist-selbstverifikation.md`: Matrix-Zeile „Parallelisierung“ abhaken oder explizit als „bewusst verschoben“ markieren.

## Nächster Schritt

`04-yaml-escaping-zentralisieren.md` oder `05-signatures-rewriter-speicher-druck-optional.md`.
