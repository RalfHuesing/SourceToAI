# Task 07: README & Nutzerhinweise

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Repo-`README.md` (und falls vorhanden generierte Export-`readme.md`-Logik) nur **dann** anpassen, wenn sich **sichtbares** Verhalten ändert:
  - fehlertolerantes Scanning (`02`) → z. B. Hinweis, dass bei gesperrten Ordnern eine Teilmenge exportiert wird;
  - Parallelismus (`03`) → optional Hinweis auf nicht-deterministische Konsolen-Reihenfolge vs. stabile Dateiausgabe (falls relevant).
- Keine Marketing-Texte; kurze, technische Klarstellung.

## Nicht-Ziel

- Keine Umbenennung der View-Ordner oder Export-Struktur (liegt außerhalb dieses Epics).

## Querschnitt Task 02 (bereits umgesetzt)

- **Repo-`README.md`:** Kurzbeschreibung „Fehlertolerantes Projekt-Scanning“ in den Features (Teilmenge statt komplettem Ausfall).
- **CLI:** `ConsoleOrchestrator` gibt bei übersprungenen Pfaden `[WARN]` pro Projektzeile aus (`ExtractionResult.Warnings` von `FindFilesForProject`).

## Abhängigkeiten

- Nach `02` und ggf. `03` prüfen, ob Nutzerhinweis nötig ist; sonst Task als „nicht anwendbar“ in `00` vermerken und hier alle Checkboxen mit Begründung abhaken.

## Tests (Pflicht)

- Keine separaten Tests für Markdown-Doku; inhaltliche Korrektheit manuell gegen tatsächliches Verhalten prüfen.

## Selbstverifikation (nach Umsetzung)

- [x] README-Konsistenz mit implementiertem Verhalten.
- [x] `00-epic-master-checklist-selbstverifikation.md`: Matrix-Zeile „Nutzerhinweise“ abhaken oder „n/a“ dokumentiert.

## Nächster Schritt

Epic in `00-epic-master-checklist-selbstverifikation.md` auf **abgeschlossen** gesetzt; `konzept.md` bei Bedarf um Status-Zeile ergänzen (optional).
