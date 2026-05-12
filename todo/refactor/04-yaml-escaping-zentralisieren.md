# Task 04: YAML-Escaping zentralisieren

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- `EscapeYamlDoubleQuoted` (aktuell u. a. privat in `AiFeedMarkdownComposer`) zentral bereitstellen; prüfen, ob `MarkdownFeedGenerator` (YAML `project:` aktuell ohne dieselbe Escaping-Logik) **angleichen** soll, damit Sonderzeichen in `feedName` nicht die YAML-Validität brechen (`konzept.md` Abschnitt 4, letzter Bullet).
- Benennung und Ort am bestehenden Projekt ausrichten (z. B. kleine statische Hilfsklasse unter `Services/Export/AiFeed` oder dediziertes Interface, falls DI gewünscht).

## Nicht-Ziel

- Kein generischer YAML-Serializer und kein vollständiger „YAML-Builder“ (YAGNI laut `konzept.md`).

## Abhängigkeiten

- Keine harten Abhängigkeiten; kann unabhängig von `01`–`03` erfolgen.

## Tests (Pflicht)

- Unit-Tests für Escaping: Sonderzeichen, Zeilenumbrüche, Anführungszeichen, leerer String — konsistent mit bisherigem Verhalten (Regression).

## Selbstverifikation (nach Umsetzung)

- [ ] `dotnet build` / `dotnet test` grün.
- [ ] `00-epic-master-checklist-selbstverifikation.md`: Matrix-Zeile „YAML-Escaping“ abhaken.
- [ ] Keine Warnungen neu eingeführt.

## Nächster Schritt

`05-signatures-rewriter-speicher-druck-optional.md` oder `06-tests-querschnitt-regression-und-benchmark-notizen.md`.
