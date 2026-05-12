# Task 05 (optional): SignaturesRewriter — Speicherdruck / Roslyn-Allokationen

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- `konzept.md` Abschnitt 3 (Memory): wiederholtes `WithBody(null).WithExpressionBody(null)` im `SignaturesRewriter` **bewerten** — zuerst Profiling oder große Fixture, dann gezielte Reduktion von Zwischenbäumen, falls messbar sinnvoll.
- Bei Refactoring: **500-Zeilen-Regel** der Projektrichtlinien beachten — ggf. Typ splitten statt weiter anzureichern.

## Nicht-Ziel

- Keine Mikro-Optimierung ohne Messung.
- Keine Änderung der semantischen Ausgabe der `signatures-only`-View ohne Test-Anpassung.

## Abhängigkeiten

- Sinnvoll nach `01` und idealerweise nach einem kurzen Baseline-Lauf (`06`).

## Tests (Pflicht, wenn Code geändert wird)

- Bestehende Tests für Signatures-/View-Builder grün; bei Verhaltensänderung Tests erweitern.

## Selbstverifikation (nach Umsetzung)

- [x] Kurznotiz: vorher/nachher (z. B. Allocations oder Laufzeit) oder bewusst „kein messbarer Gewinn → Task verworfen“ in `00` dokumentiert.
- [x] `dotnet test` grün.

## Nächster Schritt

`06-tests-querschnitt-regression-und-benchmark-notizen.md`.
