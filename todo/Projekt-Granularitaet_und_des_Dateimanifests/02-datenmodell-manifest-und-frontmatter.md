# Task 02: Datenmodell — Manifestzeile, Dateityp, Hash, Frontmatter-Felder

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Unveränderliche **DTOs** (Records oder schlanke Klassen) für:
  - eine **Manifestzeile**: fortlaufende `Id`, `Type` (`Code` / `Doc` o. Ä. laut Konzept), `Hash` (8 Zeichen, Konzept: MD5 — **Klärung:** vollständiger MD5 hex gekürzt vs. anderes; im Code festhalten und testen), `Size` in Bytes (UTF-8 der **exportierten** Zeichenkette oder Rohbytes — **eine** Definition wählen und dokumentieren), `Path` relativ zum Projektroot mit Verzeichnistrenner wie im Konzept-Beispiel (`\` vs `/`: unter Windows-Konvention oder durchgängig `/` — festlegen).
  - **Frontmatter-Metadaten**: `feed_type: source_export`, `project: "<Solution> (<Projekt>)"` exakt nach Konzept-Syntax, `session_id` (Guid pro Dokument), `generated` (ISO-8601), `file_count` (Anzahl der Manifestzeilen = enthaltene Dateien).
- Keine String-Zusammenstellung des finalen Markdowns in diesem Task — nur saubere Modelle + ggf. Hilfsfunktionen (Hash berechnen aus `string` oder `ReadOnlySpan<byte>`).

## Nicht-Ziel

- Kein vollständiger `AiFeed`-Composer (Task `03`).
- Keine Orchestrierung.

## Abhängigkeiten

- Optional: `01` für konsistente Anzeigenamen — kann parallel starten, vor `03` zusammenführen.

## Tests (Pflicht)

- Unit-Tests: Hash für bekannten kurzen Input → erwarteter 8-Zeichen-Präfix (falls MD5).
- `file_count`-Logik: leere Liste → 0; drei Einträge → 3.

## Selbstverifikation (nach Umsetzung)

- [x] Modelle im passenden Namespace (z. B. `Services/Export/AiFeed/` oder `Models/`) — konsistent mit bestehendem Projekt.
- [x] `00`-Matrix: Zeilen Frontmatter/Manifest mit `02`/`03` verknüpft.
- [x] `dotnet test` grün.

## Nächster Schritt

`03-zentraler-ai-feed-markdown-composer.md`
