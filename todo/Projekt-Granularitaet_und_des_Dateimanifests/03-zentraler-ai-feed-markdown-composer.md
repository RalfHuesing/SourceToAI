# Task 03: Zentraler AI-Feed-Markdown-Composer (YAML + INSTRUCTION + MANIFEST + CONTENT)

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken.

## Ziel

- Interface z. B. `IAiFeedMarkdownComposer` mit Methode, die aus:
  - Solution-/Projekt-Anzeigenamen,
  - einer Liste **bereits gefilterter** Inhaltssegmente (relativer Pfad, Sprache/Fence-Tag, Text nach View-Transformation),
  - und den in `02` definierten Metadaten  
  **ein fertiges Markdown-Dokument** erzeugt gemäß `Konzept.md` Abschnitt 3 (A–D).
- Struktur strikt:
  - **A)** YAML-Frontmatter mit `---` Begrenzung.
  - **B)** `# AI FEED: …` und `## INSTRUCTION` mit dem im Konzept vorgegebenen Text (ggf. kleine Anpassungen nur nach Rücksprache im Chat).
  - **C)** `## MANIFEST` + Tabelle mit Spalten `ID | Type | Hash | Size | Path`; erste Spalte als Markdown-Link `[n](#n)`.
  - **D)** `## CONTENT` + Abschnitte `### [n] <Pfad>` + Fencing; **Backtick-Anzahl** über `MarkdownFenceUtility.CalculateRequiredBackticks` (mindestens 4 wenn nötig).
- Eine Implementierungsklasse, registriert in `Program.cs`.

## Nicht-Ziel

- Keine View-spezifische Roslyn-Logik.
- Kein Dateischreiben (macht Aufrufer `06`).

## Abhängigkeiten

- `02` (Manifest-/Metadaten-Modell) sollte vorher oder in einem gemeinsamen PR fertig sein.

## Tests (Pflicht)

- Unit-Tests mit **Golden-String** oder strukturierten Teil-Asserts:
  - Frontmatter enthält alle Keys.
  - Manifest hat N Zeilen + Header; CONTENT hat N `### [k]`-Überschriften.
  - Quelltext mit vielen Backticks → Fence mindestens 4 Zeichen breit.
- Edge: genau **null** Inhaltssegmente nach Filter — definiertes Verhalten (leeres Manifest? Platzhaltertext? — mit Task `05` abstimmen; minimal: kein Crash, gültiges Markdown).

## Selbstverifikation (nach Umsetzung)

- [ ] Composer ist die **einzige** Stelle, die Tabellenlayout und Überschriftenhierarchie A–D festlegt.
- [ ] `dotnet test` grün.
- [ ] `00`-Matrix abhaken.

## Nächster Schritt

`04-view-builder-liefert-segmente-composer-integriert.md`
