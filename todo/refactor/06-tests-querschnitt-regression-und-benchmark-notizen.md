# Task 06: Tests — Querschnitt, Regression, Nachweise

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Alle Änderungen aus `01`–`05` mit **Regressionssicherheit** absichern.
- Expliziter Nachweis für **Parse Once** (Zähler-Mock, oder dokumentierter manueller Nachweis, der im Repo nachvollziehbar ist — bevorzugt automatisierter Test).
- `MultiViewExportIntegrationTests`, `AiFeedProjectGranularityIntegrationTests` und verwandte App-Tests nach jedem größeren Merge-Schritt grün halten.

## Nicht-Ziel

- Kein vollständiges Performance-Lab im CI (optional: einfache Notiz in dieser Datei oder im PR, wo lokal gemessen wurde).

## Abhängigkeiten

- Sollte **nach** den inhaltlichen Tasks `01`/`02`/`04` (und optional `03`/`05`) als Abschluss-Querschnitt durchlaufen werden.

## Tests (Pflicht)

- `dotnet test` auf Solution-Ebene grün.
- Neue Tests nur dort, wo fachliche Lücken entstehen (siehe Teil-Tasks).

## Selbstverifikation (nach Umsetzung)

- [ ] `dotnet test` grün (lokal/CI).
- [ ] `00-epic-master-checklist-selbstverifikation.md`: finale Matrix- und Abschluss-Checkboxen mitverifizieren.
- [ ] Parse-Once-Nachweis verlinkt oder in Test-Klassenname/Kommentar auffindbar.

## Nächster Schritt

`07-readme-und-nutzerhinweise.md`.
