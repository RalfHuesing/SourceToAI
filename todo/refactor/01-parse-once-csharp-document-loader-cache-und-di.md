# Task 01: Parse Once — gemeinsamer C#-Dokument-Cache & DI-Lebensdauer

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- **Eine** logische Cache-Ebene für bereits gelesene und geparste `.cs`-Dateien (Key: normalisierter absoluter Pfad, `StringComparer.OrdinalIgnoreCase`), wie in `konzept.md` Abschnitt 4 skizziert.
- Sicherstellen, dass **alle** `IMarkdownProjectViewBuilder`-Implementierungen im selben Exportlauf **dieselbe** Cache-Instanz nutzen (Problem heute: `AddTransient<ICSharpDocumentLoader, CSharpDocumentLoader>()` × mehrere Transient-Builder → effektiv mehrere Loader ohne gemeinsamen Cache).
- Verhalten der öffentlichen API von `ICSharpDocumentLoader` / `LoadParsedDocuments` beibehalten, soweit möglich; bei nötiger Erweiterung (z. B. `Clear()` für Tests) Interface dokumentieren.

## Nicht-Ziel

- Kein Umbau der View-spezifischen Rewrite-Logik (Signatures, public-only, …) über den notwendigen Anpassungsgrad an der Datenquelle hinaus.
- Kein vorschnelles **Parallel-Parsing** innerhalb dieser Task (siehe Task `03`).

## Abhängigkeiten

- Keine Blocker; sollte vor oder zusammen mit messbaren Performance-Tests (`06`) abgeschlossen werden.

## Tests (Pflicht)

- **Unit:** `CSharpDocumentLoaderTests` erweitern oder ergänzen — gleicher Pfad zweimal in einem Loader mit gemeinsamem Cache → `IFileReader.ReadAllText` nur einmal (Mock/FileReader-Zähler).
- **Integration:** Sicherstellen, dass Multi-View-Export weiterhin identische inhaltliche Erwartungen erfüllt (bestehende Tests grün); optional separater Test mit Zähler-Mock über DI-Testhost.

## Selbstverifikation (nach Umsetzung)

- [x] `dotnet build` / `dotnet test` grün.
- [x] `00-epic-master-checklist-selbstverifikation.md`: Matrix-Zeile „Parse Once“ abhaken.
- [x] Keine Warnungen neu eingeführt.
- [x] Kurz verifiziert: **pro .cs-Datei** im Projekt maximal ein Read+Parse über **alle** Views im selben Lauf.

## Nächster Schritt

`02-file-discovery-fehlertolerantes-scanning.md` (unabhängig parallel möglich: `04-yaml-escaping-zentralisieren.md`).
