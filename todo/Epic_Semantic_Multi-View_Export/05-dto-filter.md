# Task 05: `DtoFilter` — nur Models/DTOs/Enums/Records

> **Pflicht bei Umsetzung:** Wenn diese Task umgesetzt oder nachträglich verifiziert wurde, alle zutreffenden `- [ ]` in **dieser** Datei auf `- [x]` setzen. Betrifft es die Master-Checkliste `00-epic-master-checklist-selbstverifikation.md`, dort die passenden Punkte **ebenfalls** abhaken — nicht nur im Chat oder im Commit beschreiben.

## Ziel

- Logik (Rewriter **oder** separater Filter), die bestimmt, ob eine **Typ-Deklaration** in die DTO-View gehört:
  - `enum`: behalten.
  - `record` / `record class` / `record struct`: behalten (Konzept: „records“).
  - **Klassen:** nur wenn **keine** „komplexen“ Methoden — Heuristik wie im Konzept: im Wesentlichen **Properties** und **Felder**; ggf. trivialer Konstruktor, `ToString`-Override? **Festlegen:** z. B. erlaubt: property-like Methoden (`get_X`) nein — strikt: nur auto-properties + Felder + leerer ctor.
- Nicht-DTO-Klassen: komplette Typ-Deklaration aus der Compilation Unit entfernen (oder ganze Datei auslassen, wenn nach Filter nichts übrig bleibt — in `06` im Markdown klar: leere Abschnitte vermeiden).

## Abhängigkeiten

- `01`, `02`.

## Tests (Pflicht)

- Positiv: `class OrderDto { public int Id { get; set; } }`
- Negativ: `class Service { public void Run() { } }`
- Grenzfall: `class X { public int P { get; set; } public string M() => ""; }` → **muss raus**.
- `enum E { A, B }` bleibt.
- `record R(int A);` bleibt.

## Selbstverifikation

- [ ] Heuristik in einem Satz in XML-Doc am öffentlichen Entry-Point dokumentiert.
- [ ] `models.md` wird nicht mit vollen Services gefüllt (Tests).
- [ ] `00`-Matrix Zeile DTO abgehakt.
- [ ] `dotnet test` grün.

## Nächster Schritt

`06-markdown-view-builder.md`
