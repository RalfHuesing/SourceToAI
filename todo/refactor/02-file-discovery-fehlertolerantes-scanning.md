# Task 02: File Discovery — fehlertolerantes Verzeichnis-Scanning

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- `FileDiscoveryService.ScanDirectory` so gestalten, dass **ein** nicht lesbares Verzeichnis oder eine gesperrte Datei nicht den gesamten `FindFilesForProject`-Pfad als Fehler beendet (`konzept.md` Abschnitt 2).
- Konkret: `UnauthorizedAccessException` (und nach sinnvoller Prüfung ggf. weitere I/O-Fehler) **lokal** abfangen, überspringen, mit dem Rest fortfahren.
- Optional: strukturierte Warnung (Logging-Konzept des Projekts prüfen — falls noch `Console.WriteLine`, konsistent halten) statt stiller Ignorierung, ohne das CLI unübersichtlich zu fluten.

## Nicht-Ziel

- Kein vollständiges „Recovery“-Reporting mit JSON-Audit-Trail (YAGNI).
- Keine Änderung der Include-/Exclude-Regeln (`AppSettings`), außer es wird zum Fix zwingend benötigt.

## Abhängigkeiten

- Keine.

## Tests (Pflicht)

- `FileDiscoveryServiceTests`: Szenario mit temporärem Verzeichnis — ein Unterordner simuliert fehlende Berechtigung (z. B. durch Test-Double oder OS-spezifisches Setup); **erwartet:** übrige Dateien werden gefunden, Ergebnis `Success` mit Teilliste (oder dokumentiertes `Failure`-Verhalten nur bei totalem Ausfall — im Konzept ist Erfolg mit Teilmenge intendiert; in der Task-Implementierung einheitlich festlegen und testen).

## Selbstverifikation (nach Umsetzung)

- [ ] `dotnet build` / `dotnet test` grün.
- [ ] `00-epic-master-checklist-selbstverifikation.md`: Matrix-Zeile „Abbruch ganzer Projekte“ abhaken.
- [ ] Keine Warnungen neu eingeführt.
- [ ] Verhalten in `07-readme-und-nutzerhinweise.md` erwähnt, falls Nutzer sichtbare Ausgabe/Warnung bekommt.

## Nächster Schritt

`03-parallelismus-export-orchestrator-und-roslyn.md` oder `06-tests-querschnitt-regression-und-benchmark-notizen.md` (Verifikation nach `01`).
