# Task 06: MultiViewExportService — Schleife Projekte → Views, eine Datei pro Kombination

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- `WriteMergedSolutionViews` (oder Nachfolge-API) so umbauen, dass **nicht mehr** pro View ein aggregierter `StringBuilder` über alle Projekte geschrieben wird.
- Stattdessen: äußere Schleife über **Projekte** (stable Sort beibehalten), innere über **Views** — oder umgekehrt, solange das Ergebnis **pro Projekt und View genau eine Ausgabedatei** ist und `Konzept.md` die semantische Reihenfolge „pro Projekt alle Views“ nicht verletzt.
- Ausgabepfad: `Path.Combine(outputRoot, viewFolder, <Dateiname aus Task 01>)`.
- Virtual-Projekt **`.Docs`** / Solution-Dokumentation: wie bisher nur sinnvoll in `complete`; klären, ob eigenständige „Projekt“-Datei `SolutionName..Docs.md` o. Ä. oder anderer Name — **konsistent mit `01`** und im README erwähnen.
- Entfernen oder Anpassen von `RelativeOutputFile` auf den konkreten Buildern, falls nur noch der Ordner pro View relevant ist (pro Datei dynamischer Name).

## Nicht-Ziel

- Keine Änderung an `ConsoleOrchestrator`-Reihenfolge außerhalb des `IMultiViewExportService`-Vertrags, außer Signatur muss erweitert werden (dann `ConsoleOrchestrator` + Interface in diesem Task oder klar getrenntem Follow-up mitverdrahten).

## Abhängigkeiten

- `01` (Pfade/Dateinamen), `03`–`05` (fertiger Dokumentinhalt pro Projekt/View).

## Tests (Pflicht)

- Anpassung `MultiViewExportIntegrationTests` / ähnliche: erwarte **mehrere** Dateien unter `complete/` statt einer `full-source.md`.
- Mindestens: zwei Projekte → mindestens zwei Dateien unter `complete/` (sofern beide Dateien haben).

## Selbstverifikation (nach Umsetzung)

- [x] Keine alten Pfade `full-source.md` / `signatures.md` als Pflicht mehr (README/Task `07`).
- [x] `dotnet test` grün.
- [x] `00`-Matrix und Verzeichnisbaum-Beispiel in `00` mit Realität abgehakt/abgeglichen.

## Nächster Schritt

`07-readme-und-nutzerhinweise.md`
