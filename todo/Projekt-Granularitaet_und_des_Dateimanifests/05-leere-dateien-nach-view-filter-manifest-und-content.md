# Task 05: Leere Dateien nach View-Filter — vollständig aus Manifest und CONTENT entfernen

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Konzept wörtlich umsetzen: Wenn eine Datei durch einen View-Filter **vollständig leer** wird (nur Whitespace / leerer String / semantisch „nichts zu exportieren“ — **Kriterium im Code kommentieren**), dann:
  - **keine** Zeile in `## MANIFEST`,
  - **kein** Block in `## CONTENT`.
- Abstimmung mit `03`: Wenn **alle** Dateien eines Projekts in einer View wegfallen → gesamtes Markdown-Dokument für diese Kombination (Projekt+View) entweder **nicht schreiben** oder eine kurze definierte Stub-Datei — **eine** Variante wählen, in `00` und README dokumentieren (empfohlen: Datei weglassen oder nur schreiben, wenn mindestens ein Segment; Orchestrator-Log optional).

## Nicht-Ziel

- Keine neue Filterheuristik für Roslyn (nur Anwendung des bestehenden View-Ergebnisses).

## Abhängigkeiten

- `03`, `04` (Segmente + Composer müssen filterbar sein).

## Tests (Pflicht)

- Unit- oder Integrationstest mit Fixture: Datei, die in `public-only` komplett entfällt → Manifest ohne diesen Pfad; CONTENT ohne Abschnitt; ID-Vergabe lückenlos **1..k** ohne Lücken für verbleibende Dateien.
- Gegenprobe `complete`: dieselbe Datei weiterhin enthalten (sofern im Projekt vorhanden).

## Selbstverifikation (nach Umsetzung)

- [x] Verhalten für „Projekt ohne exportierbare Dateien in dieser View“ dokumentiert und getestet.
- [x] `dotnet test` grün.
- [x] `00`-Matrix: Zeile „leere Dateien“ abhaken.

## Nächster Schritt

`06-multiview-export-orchestration-projekte-dann-views.md`
